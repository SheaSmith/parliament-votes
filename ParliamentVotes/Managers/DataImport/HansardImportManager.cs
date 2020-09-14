using AngleSharp;
using AngleSharp.Dom;
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Data;
using ParliamentVotes.Models.Motions;
using ParliamentVotes.Models.Organisational;
using ParliamentVotes.Models.Votes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParliamentVotes.Managers.DataImport
{
    public class HansardImportManager
    {
        private readonly ApplicationDbContext db;
        private readonly static string[] emptyMotions = new string[] { "That the amendment be agreed to.", "That the amendments be agreed to.", "That the amendments to the amendments be agreed to.", "That the amendment to the amendments be agreed to.", "That the amendment to the amendment be agree to.", "That the amendments to the amendment be agreed to.", "That the motion be agreed to.", "That the bill be now read a first time" };

        public HansardImportManager(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task ImportFromHansard(string url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var questionDocument = await context.OpenAsync(url);

            TimeZoneInfo nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");

            DateTime sittingDate = DateTime.ParseExact(questionDocument.QuerySelector(".publish-date").TextContent.Replace("Sitting date:", "").Trim(), "d MMM yyyy", new CultureInfo("en-NZ"));
            sittingDate = TimeZoneInfo.ConvertTimeToUtc(sittingDate, nzst);

            Parliament currentParliament = db.Parliaments.First(p => p.StartDate < sittingDate && (p.EndDate == null || p.EndDate > sittingDate));

            var debates = questionDocument.QuerySelectorAll(".hansard__level li");

            // If the debate nodes aren't null, then we have the newer Hansard format
            if (debates != null)
            {
                await ProcessHansardV2LBL(sittingDate, debates, currentParliament);
            }
        }

        public async Task GetUrls()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync("https://www.parliament.nz/en/pb/hansard-debates/rhr/");

            int number = int.Parse(document.QuerySelector(".listing-result").TextContent.Split(" ").LastOrDefault());
            double pages = Math.Ceiling(Convert.ToDouble(number) / 20.0);

            for (int i = 1; i <= pages; i++)
            {
                if (i != 1)
                {
                    document = await context.OpenAsync("https://www.parliament.nz/en/pb/hansard-debates/rhr/?Criteria.PageNumber=" + i);
                }

                var urls = document.QuerySelectorAll(".hansard__heading a");

                foreach (var url in urls)
                {
                    if (url.Attributes["title"].Value.Contains(" - Volume "))
                        await ImportFromHansard("https://www.parliament.nz" + url.Attributes["href"].Value);
                }
            }
        }

        private async Task ProcessHansardV2LBL(DateTime sittingDate, IHtmlCollection<IElement> speeches, Parliament currentParliament)
        {
            string lastQuestionTitle = null;
            string lastQuestionSubtitle = null;
            string lastQuestionDescription = null;
            string lastClause = null;
            Member lastMember = null;

            string lastQuestionCommitteeTitle = null;

            DateTime lastDate = sittingDate;
            TimeZoneInfo nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");

            Stage? lastStage = null;
            QuestionType lastQuestionType = QuestionType.Motion;

            List<Question> newQuestions = new List<Question>();

            foreach(var speech in speeches)
            {
                // Process the speeches line by line
                var sectionNode = speech.QuerySelector(".section");

                if (sectionNode == null)
                    continue;

                var speechLines = sectionNode.Children;

                foreach (var speechLine in speechLines)
                {
                    // If the node is of type 'Speech' then we have a time node we can use
                    if (speechLine.ClassList.Contains("Speech"))
                    {
                        // Get the node that contains the time in the 'name' attribute
                        var timeNode = speechLine.QuerySelector("a");
                        if (timeNode != null)
                        {
                            // Remove the time_ prefix
                            string time = timeNode.Attributes["name"].Value.Replace("time_", "");
                            try
                            {
                                // Parse the date
                                DateTime speechTime = DateTime.ParseExact(time, "yyyyMMdd HH:mm:ss", new CultureInfo("en-NZ"));
                                // Convert the date to UTC
                                lastDate = TimeZoneInfo.ConvertTimeToUtc(speechTime, nzst);
                            } catch (Exception e)
                            {

                            }
                        }
                    }

                    // If the line contains the title of the debate
                    if (speechLine.ClassList.Any(c => c == "Debate" || c == "BillDebate" || c == "Debatealone"))
                    {
                        // Remove excess whitespace and double spaces
                        string newQuestionTitle = speechLine.TextContent.Trim().Replace("  ", " ");

                        // Set the question title to title case
                        newQuestionTitle = new CultureInfo("en-NZ").TextInfo.ToTitleCase(newQuestionTitle.ToLower());

                        // If the question title has changed, then we have moved to a different item of business
                        if (lastQuestionTitle == null || newQuestionTitle.ToLower() != lastQuestionTitle.ToLower())
                        {
                            lastQuestionSubtitle = null;
                            lastStage = null;
                            lastQuestionType = QuestionType.Motion;
                            lastQuestionTitle = newQuestionTitle;
                        }
                    }

                    // If the line contains the subtitle of the debate
                    if (speechLine.ClassList.Any(c => c == "SubDebate" || c == "SubDebatealone"))
                    {
                        lastQuestionSubtitle = speechLine.TextContent.Replace(" (continued)", "").Replace("  ", " ").Trim();

                        lastQuestionCommitteeTitle = null;

                        // If the last title was simply 'motions' give a more descriptive title
                        if (lastQuestionTitle == "Motions")
                        {
                            lastQuestionTitle = lastQuestionSubtitle;
                            lastQuestionSubtitle = null;
                        }
                    }

                    // If there is a committee topic (specifically for the Estimates debate)
                    if (speechLine.ClassList.Contains("MarginHeading"))
                    {
                        lastQuestionCommitteeTitle = speechLine.TextContent.Replace(" (continued)", "").Replace("  ", " ").Trim();
                    }

                    string speechContent = speechLine.TextContent;
                    // Look for any relevant motions
                    if ((speechContent.Contains("I move") || speechContent.Contains("The question now is") || speechContent.Contains("The question was put that")) && speechLine.ClassList.Any(c => c == "Speech" || c == "ContinueSpeech" || c == "a"))
                    {
                        // Gather all of the italics text in this line
                        var motionNodes = speechLine.QuerySelectorAll("em");

                        string motion = null;
                        // If we have the appropriate nodes, then we need to set the motion
                        if (motionNodes != null && motionNodes.Count() != 0)
                            // Join the parts of the motion together
                            motion = string.Join("", motionNodes.Select(m => m.TextContent)).Trim();

                        // Ensure the motion starts with 'That'
                        if (motion != null && !motion.StartsWith("That"))
                            motion = null;

                        if (motion != null)
                            lastQuestionDescription = motion;

                        if (speechLine.Children.Any(c => c.TagName == "strong"))
                        {
                            var memberMoving = speechLine.Children.FirstOrDefault(c => c.TagName == "strong").TextContent.Split(" (")[0].Replace("Rt Hon ", "").Replace("Hon ", "").Trim();

                            lastMember = db.Members.FirstOrDefault(m => (m.FirstName + " " + m.LastName).ToLower() == memberMoving.ToLower() || m.AlsoKnownAs.ToLower() == memberMoving.ToLower());

                            if (lastMember == null)
                                throw new Exception("Mover not found in DB, member name: " + memberMoving);
                        }
                    }

                    // The motion lapsed, so clear the motion
                    if (speechContent.ToLower().Contains("motion lapsed"))
                    {
                        lastQuestionDescription = null;
                        lastMember = null;
                    }

                    // Any clauses. They only may be preceded by a motion (special cases apply for committee stages)
                    if (speechLine.ClassList.Contains("clause"))
                    {
                        if (speechLine.PreviousElementSibling != null)
                        {
                            speechContent = speechLine.PreviousElementSibling.TextContent;
                            if ((speechContent.Contains("I move") || speechContent.Contains("The question now is") || speechContent.Contains("The question was put that")) && speechLine.PreviousElementSibling.ClassList.Any(c => c == "Speech" || c == "ContinueSpeech" || c == "a"))
                                lastClause = speechLine.InnerHtml;
                        }
                    }

                    // Look for any voice votes
                    if (speechLine.ClassList.Any(c => c == "IndentMarginalone" || c == "Speech" || c == "MarginHeading"))
                    {
                        string voiceVoteContent = speechLine.TextContent.Trim();

                        string voiceVoteHtml = speechLine.InnerHtml.Trim();

                        if (voiceVoteHtml.StartsWith("<strong>"))
                        {
                            voiceVoteHtml = voiceVoteHtml.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }

                        // If this line contains 'agreed to' or 'Bill read ... time' then it is likely a voice vote and if there is no extra HTML on the inside
                        if ((voiceVoteContent.EndsWith(" agreed to.") || (voiceVoteContent.StartsWith("Bill read ") && voiceVoteContent.EndsWith(" time."))) && voiceVoteHtml == voiceVoteContent)
                        {
                            //// Specific clause for committees, when integrated into motion
                            //var clauseTitle = voiceVoteNode.QuerySelector("strong");

                            //if (clauseTitle != null)
                            //    lastQuestionSubtitle = clauseTitle.TextContent.Replace("  ", " ").Trim();

                            // The previous node might have a better motion description
                            var previousNode = speechLine.PreviousElementSibling;

                            // If the previous node exists, but its a clause for an ammendment, then we save that clause
                            if (previousNode != null && previousNode.ClassList.Contains("clause"))
                            {
                                lastClause = previousNode.InnerHtml;
                                previousNode = previousNode.PreviousElementSibling;
                            }

                            // The next node might be a party vote
                            var nextNode = speechLine.NextElementSibling;

                            // If the nextNode node exists, but its a clause for an ammendment, then we save that clause
                            if (nextNode != null && nextNode.ClassList.Contains("clause"))
                            {
                                lastClause = nextNode.InnerHtml;
                                nextNode = nextNode.NextElementSibling;

                                // If the nextNode node exists, but its a table for a schedule
                                if (nextNode != null && nextNode.ClassList.Contains("tablescroll"))
                                {
                                    lastClause += nextNode.InnerHtml;
                                    nextNode = nextNode.NextElementSibling;
                                }
                            }

                            if (previousNode != null && previousNode.TextContent.Contains("The question was put that "))
                            {
                                lastQuestionDescription = previousNode.TextContent.Replace("The question was put that ", "That ").Trim();
                                // Remove the period from the end
                                lastQuestionDescription = lastQuestionDescription.Substring(0, lastQuestionDescription.Length - 1);
                            }
                            else if (voiceVoteContent.Contains("The question was put that "))
                            {
                                lastQuestionDescription = voiceVoteContent.Replace("The question was put that ", "That ").Trim();
                                // Remove the period from the end
                                lastQuestionDescription = voiceVoteContent.Substring(0, voiceVoteContent.Length - 1);
                            }
                            else if (lastQuestionDescription == null)
                            {
                                // As a last resort, use the result of the vote as a description, but remove the agreement information
                                lastQuestionDescription = voiceVoteContent.Replace(" not agreed to.", "").Replace(" agreed to.", "").Trim();
                            }

                            if ((previousNode != null && previousNode.ClassList.Contains("VoteText") && (lastQuestionDescription.Contains("read") || lastQuestionDescription.Contains("agreed"))) || (nextNode != null && nextNode.ClassList.Contains("VoteReason") && voiceVoteContent.Contains("The question was put that ")))
                            {
                                // Remove the last question description and clause, as there should only be one question for each of these
                                lastQuestionDescription = null;
                                lastMember = null;
                                lastQuestionType = QuestionType.Motion;
                                lastClause = null;
                                // This is actually the result of a party vote that has been miscategorised, so skip it
                                continue;
                            }

                            // Extract various information about the question from the description and subtitle
                            var extractionResults = GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle, lastQuestionDescription);
                            lastQuestionType = extractionResults.Item2;
                            lastStage = extractionResults.Item1 == null ? lastStage : extractionResults.Item1;
                            lastQuestionTitle = extractionResults.Item3 == null ? lastQuestionTitle : extractionResults.Item3;

                            // Create the question
                            Question question = new Question();
                            newQuestions.Add(question);

                            // If the motion is simply about votes, then provide more detail
                            if (lastQuestionDescription == "Votes" && lastQuestionCommitteeTitle != null)
                            {
                                lastQuestionDescription = lastQuestionCommitteeTitle;
                            }

                            // Update the question as necessary
                            question.Title = lastQuestionTitle;
                            question.Subtitle = lastQuestionSubtitle;
                            question.Description = lastQuestionDescription;
                            question.Timestamp = lastDate;
                            question.Stage = lastStage;
                            question.QuestionType = lastQuestionType;
                            question.Clause = lastClause;
                            question.Member = extractionResults.Item4 == null ? lastMember : extractionResults.Item4;
                            question.Parliament = currentParliament;

                            // Find if there is a bill associated with this question
                            if (question.Title.Contains(" Bill"))
                            {
                                var bill = db.Bills.FirstOrDefault(b => b.Title.ToLower() == question.Title.ToLower());

                                if (bill != null)
                                {
                                    question.Title = bill.Title;
                                    question.Bill = bill;
                                }
                            }

                            // Find if there is a SOP associated with this question
                            Match sopMatch = Regex.Match(question.Description, @"Supplementary Order Paper ([0-9]+)");
                            if (sopMatch.Success && !question.Description.Contains(" amendment "))
                            {
                                var sopNumber = int.Parse(sopMatch.Groups[1].Value);

                                var sop = db.SupplementaryOrderPapers.FirstOrDefault(s => s.Number == sopNumber);

                                if (sop != null)
                                {
                                    question.SupplementaryOrderPaper = sop;
                                    question.Member = sop.Member;
                                }
                            }

                            // See if this voice vote already exists
                            VoiceVote vote = db.VoiceVotes.FirstOrDefault(v => v.Question_Id == question.Id);

                            // If the vote doesn't exist, then we need to create a new one
                            if (vote == null)
                            {
                                vote = new VoiceVote();
                                db.VoiceVotes.Add(vote);
                            }

                            // Update the vote
                            vote.Question = question;
                            vote.Position = !voiceVoteContent.Contains(" not agreed to");

                            // Remove the last question description and clause, as there should only be one question for each of these
                            lastQuestionDescription = null;
                            if (extractionResults.Item4 == null)
                                lastMember = null;
                            lastQuestionType = QuestionType.Motion;
                            lastClause = null;
                        }
                    }

                    // Look for any party or personal votes
                    if (speechLine.ClassList.Contains("VoteReason"))
                    {
                        var questionDescriptionNodes = speechLine.QuerySelectorAll("em");

                        string motion = null;

                        if (questionDescriptionNodes != null && questionDescriptionNodes.Length != 0)
                            motion = string.Join("", questionDescriptionNodes.Select(q => q.TextContent)).Trim();

                        if (motion != null && (!emptyMotions.Any(m => m.ToLower() == motion.ToLower()) || lastQuestionDescription == null))
                            lastQuestionDescription = motion;

                        // The previous node might have a better motion description
                        var previousNode = speechLine.PreviousElementSibling;

                        // If the previous node exists, but its a schedule for an ammendment, then we need to go back
                        if (previousNode != null && previousNode.ClassList.Any(c => c == "table" || c == "tablescroll"))
                        {
                            lastClause = previousNode.InnerHtml;
                            previousNode = previousNode.PreviousElementSibling;
                        }

                        // If the previous node exists, but its a clause for an ammendment, then we simply save that clause
                        if (previousNode != null && previousNode.ClassList.Contains("clause"))
                        {
                            lastClause = previousNode.InnerHtml + (lastClause == null ? "" : lastClause);
                            previousNode = previousNode.PreviousElementSibling;
                        }

                        if (previousNode != null && previousNode.TextContent.Contains("The question was put that "))
                        {
                            lastQuestionDescription = previousNode.TextContent.Replace("The question was put that ", "That ").Trim();
                            // Remove the period from the end
                            lastQuestionDescription = lastQuestionDescription.Substring(0, lastQuestionDescription.Length - 1);
                        }

                        // Extract various information about the question from the description and subtitle
                        var extractionResults = GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle, lastQuestionDescription);
                        lastQuestionType = extractionResults.Item2;
                        lastStage = extractionResults.Item1 == null ? lastStage : extractionResults.Item1;
                        lastQuestionTitle = extractionResults.Item3 == null ? lastQuestionTitle : extractionResults.Item3;
                        
                        // Create a new question
                        Question question = new Question();
                        newQuestions.Add(question);

                        // Update the question as necessary
                        question.Title = lastQuestionTitle;
                        question.Subtitle = lastQuestionSubtitle;
                        question.Description = lastQuestionDescription;
                        question.Timestamp = lastDate;
                        question.Stage = lastStage;
                        question.QuestionType = lastQuestionType;
                        question.Clause = lastClause;
                        question.Member = extractionResults.Item4 == null ? lastMember : extractionResults.Item4;
                        question.Parliament = currentParliament;

                        // Find if there is a bill associated with this question
                        if (question.Title.Contains(" Bill"))
                        {
                            var bill = db.Bills.FirstOrDefault(b => b.Title.ToLower() == question.Title.ToLower());

                            if (bill != null)
                            {
                                question.Title = bill.Title;
                                question.Bill = bill;
                            }
                        }

                        // Find if there is a SOP associated with this question
                        Match sopMatch = Regex.Match(question.Description, @"Supplementary Order Paper ([0-9]+)");
                        if (sopMatch.Success && !question.Description.Contains(" amendment "))
                        {
                            var sopNumber = int.Parse(sopMatch.Groups[1].Value);

                            var sop = db.SupplementaryOrderPapers.FirstOrDefault(s => s.Number == sopNumber);

                            if (sop != null)
                            {
                                question.SupplementaryOrderPaper = sop;
                                question.Member = sop.Member;
                            }
                        }

                        // Remove any existing party votes for this question
                        var existingPartyVotes = db.PartyVotes.Where(p => p.Question_Id == question.Id);
                        db.PartyVotes.RemoveRange(existingPartyVotes);

                        // Get the first node where the vote results are actually stored
                        var voteNode = speechLine.NextElementSibling;

                        // This is where we store the positions we have detected
                        bool? lastPosition = null;
                        string lastComplexPosition = null;

                        // While we have a vote node that isn't null and while we aren't at the overall results
                        while (voteNode != null && !voteNode.ClassList.Contains("VoteResult"))
                        {
                            // Get the different possible positions
                            if (voteNode.ClassList.Contains("VoteCount"))
                            {
                                // Parse out the actual position, ignoring vote totals (as we calculate these ourselves)
                                string positionText = voteNode.TextContent.Trim();
                                Match positionMatch = Regex.Match(positionText, @"(.+)[ \n]+([0-9]+)");
                                positionText = positionMatch.Groups[1].Value.Trim();

                                lastPosition = null;
                                lastComplexPosition = null;

                                // Categorise the different positions
                                if (positionText == "Ayes")
                                    lastPosition = true;
                                else if (positionText == "Noes")
                                    lastPosition = false;
                                else if (positionText == "Abstentions")
                                    lastComplexPosition = "Abstain";
                                else
                                    lastComplexPosition = positionText;
                            }

                            else if (voteNode.ClassList.Contains("VoteText"))
                            {
                                string votersText = voteNode.TextContent.Trim();

                                // Reformat 'any other votes'
                                var otherVotesMatch = Regex.Match(votersText, @"Other [0-9]+ \((.+)\)");
                                if (otherVotesMatch.Success)
                                {
                                    // We're going to treat them as independants for the purposes of this party vote, so we need to convert their names to the correct format
                                    string otherVoters = otherVotesMatch.Groups[1].Value.Replace(", ", "; ");

                                    // Replace the 'others' section with the correct voters
                                    votersText = votersText.Replace(otherVotesMatch.Groups[0].Value, otherVoters);
                                }

                                // Weird fix for some ACT votes
                                votersText = votersText.Replace(", ACT ", "; ACT ").Replace(". ACT ", "; ACT ");

                                string[] voters = votersText.Replace(".", "").Replace(":", ";").Split(";");

                                foreach (string voter in voters)
                                {
                                    // Parse out the name of the party and how many voted in it
                                    var partyMatch = Regex.Match(voter, @"(.+) ([0-9]+)");

                                    if (partyMatch.Success && !voter.Contains("Other ("))
                                    {
                                        // Get the party name and the number of voters
                                        string partyName = partyMatch.Groups[1].Value.Trim();
                                        int partyNumbers = int.Parse(partyMatch.Groups[2].Value);

                                        // Find the party in the database
                                        var party = db.Parties.FirstOrDefault(p => p.Name == partyName || p.AlsoKnownAs.Contains(partyName));

                                        Member member = null;
                                        // If the party doesn't exist, look for a particular member
                                        if (party == null)
                                            member = db.Members.FirstOrDefault(m => (m.LastName == partyName || m.LastName + " " + m.FirstName.Substring(0, 1) == partyName || m.FirstName + " " + m.LastName == partyName || m.AlsoKnownAs.Contains(partyName)) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        if (party == null && member == null)
                                            throw new Exception("Party not found in DB. Party name: " + partyName);

                                        // Create a new party vote and add to DB
                                        PartyVote partyVote = new PartyVote();
                                        db.PartyVotes.Add(partyVote);

                                        // See if this vote is a split party vote
                                        Match splitPartyVoteMatch = Regex.Match(voter, @"(.+) ([0-9]+) \((.+)\)");

                                        // If it is, then we need to go through the different members and record their positions
                                        List<SplitPartyVote> splitPartyVotes = null;
                                        if (splitPartyVoteMatch.Success)
                                        {
                                            // This is a split party vote, and needs to be treated accordingly
                                            var members = splitPartyVoteMatch.Groups[3].Value.Split(", ");
                                            List<Member> splitMembers = db.Members.Where(m => (members.Contains(m.LastName) || members.Contains(m.LastName + " " + m.FirstName.Substring(0, 1))) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate))).ToList();

                                            splitPartyVotes.AddRange(splitMembers.Select(m => new SplitPartyVote(partyVote, m)));
                                        }

                                        // Update the party vote accordingly
                                        partyVote.Update(question, partyNumbers, lastPosition, party, complexPosition: lastComplexPosition, member: member);
                                    }
                                    // This is for a independant MP, so we need to add them individually
                                    else
                                    {
                                        // Parse out the member name if it has a prefix
                                        var memberName = voter.Replace("Independent: ", "").Trim();

                                        // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                        var member = db.Members.FirstOrDefault(m => (m.LastName == memberName || m.LastName + " " + m.FirstName.Substring(0, 1) == memberName || m.FirstName + " " + m.LastName == memberName || m.AlsoKnownAs.Contains(memberName)) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        Party party = null;
                                        // If the member doesn't exist, then it may actually be a party vote
                                        if (member == null)
                                        {
                                            // Find the party in the database
                                            party = db.Parties.FirstOrDefault(p => p.Name == memberName || p.AlsoKnownAs.Contains(memberName));
                                        }

                                        if (party == null && member == null)
                                            throw new Exception("Member not found in DB. Member name " + memberName);

                                        // Add the new party vote to the DB
                                        PartyVote partyVote = new PartyVote();
                                        db.PartyVotes.Add(partyVote);

                                        // Update the party vote
                                        partyVote.Update(question, 1, party: party, member: member, position: lastPosition, complexPosition: lastComplexPosition);
                                    }
                                }
                            }

                            else if (voteNode.ClassList.Contains("tablescroll") || voteNode.ClassList.Contains("table"))
                            {
                                var memberNodes = voteNode.QuerySelectorAll("td");

                                foreach (var memberNode in memberNodes)
                                {
                                    // Get the member name, determine if they voted by proxy and then remove the ' (P)' and 'Teller:' if necessary
                                    string memberName = memberNode.TextContent.Replace("Teller:", "").Trim();
                                    if (!string.IsNullOrEmpty(memberName))
                                    {
                                        bool isProxy = memberName.Contains("(P)");
                                        memberName = memberName.Replace("(P)", "").Trim();

                                        // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                        var member = db.Members.FirstOrDefault(m => (m.LastName == memberName || m.LastName + " " + m.FirstName.Substring(0, 1) == memberName || m.FirstName + " " + m.LastName == memberName || m.AlsoKnownAs.Contains(memberName)) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        if (member == null)
                                            throw new Exception("Member not found in DB. Member name " + memberName);

                                        // Add the new personal vote to the DB
                                        PersonalVote personalVote = new PersonalVote();
                                        db.PersonalVotes.Add(personalVote);

                                        personalVote.Proxy = isProxy;
                                        personalVote.Member_Id = member.Id;

                                        // Update the personal vote
                                        personalVote.Update(question, lastPosition, lastComplexPosition);
                                    }
                                }
                            }

                            // Move onto the next node
                            voteNode = voteNode.NextElementSibling;
                        }

                        // Clear the description and clause
                        lastQuestionDescription = null;
                        if (extractionResults.Item4 == null)
                            lastMember = null;
                        lastQuestionType = QuestionType.Motion;
                        lastClause = null;
                    }
                }
            }

            db.Questions.AddRange(newQuestions);

            await db.SaveChangesAsync();
        }

        private (Stage?, QuestionType, string, Member) GetStageQuestionTypeAndFixedTitle(string questionSubtitle, string questionDescription)
        {
            string questionTitle = null;
            QuestionType type = QuestionType.Motion;
            Stage? stage = null;
            Member member = null;

            // The bill is being read
            if (questionDescription.Contains(" read ") || (questionSubtitle != null && questionSubtitle.Contains(" Reading")))
            {
                // Extract the bill title with correct capitalisation
                int endIndex = questionDescription.IndexOf(" be now read");
                if (endIndex != -1)
                {
                    string newQuestionTitle = questionDescription.Substring(0, endIndex).Replace("That the ", "").Replace("That the", "").Replace("That ", "").Trim();
                    if (newQuestionTitle != "bill")
                        questionTitle = newQuestionTitle;
                }

                type = QuestionType.BillReading;

                if (questionDescription.Contains("first time") || (questionSubtitle != null && questionSubtitle.Contains("First ")))
                    stage = Stage.FirstReading;
                else if (questionDescription.Contains("second time") || (questionSubtitle != null && questionSubtitle.Contains("Second ")))
                    stage = Stage.SecondReading;
                else if (questionDescription.Contains("third time") || (questionSubtitle != null && questionSubtitle.Contains("Third ")))
                    stage = Stage.ThirdReading;
            }

            // The bill is in committee
            if (questionSubtitle != null && (questionSubtitle.Contains("Committee") || questionSubtitle.ToLower().Contains("part") || questionSubtitle.ToLower().Contains("schedule") || questionSubtitle.ToLower().Contains("clause")))
            {
                stage = Stage.Committee;
                type = QuestionType.BillPart;

                if (questionDescription.Contains(" the following ammendment "))
                    type = QuestionType.Amendment;
                else if (questionDescription.Contains("Supplementary Order Paper"))
                    type = QuestionType.SupplementaryOrderPaper;
                else if (questionDescription.Contains("ammendment"))
                    type = QuestionType.Amendment;
                else if (questionDescription.Contains("question"))
                    type = QuestionType.Motion;

                Match memberNameMatch = Regex.Match(questionDescription.Replace(" the Rt Hon ", "").Replace(" the Hon ", ""), @"((?:[A-Z][^\s]*\s?)+)");

                if (memberNameMatch.Success)
                {
                    string memberName = memberNameMatch.Groups[1].Value.Trim();

                    member = db.Members.FirstOrDefault(m => (m.FirstName + " " + m.LastName).ToLower() == memberName.ToLower() || m.AlsoKnownAs.ToLower() == memberName.ToLower());

                    if (member == null)
                        throw new Exception("Mover not found in DB, member name: " + memberName);
                }
            }

            return (stage, type, questionTitle, member);
        }
    }
}
