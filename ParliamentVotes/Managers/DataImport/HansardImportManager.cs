using AngleSharp;
using AngleSharp.Dom;
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
        private readonly ApplicationDbContext _db;

        private static readonly string[] EmptyMotions =
        {
            "That the amendment be agreed to.", "That the amendments be agreed to.",
            "That the amendments to the amendments be agreed to.", "That the amendment to the amendments be agreed to.",
            "That the amendment to the amendment be agree to.", "That the amendments to the amendment be agreed to.",
            "That the motion be agreed to.", "That the bill be now read a first time"
        };

        public HansardImportManager(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ImportFromHansard(string url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var questionDocument = await context.OpenAsync(url);

            TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

            DateTime sittingDate = new DateTime();
            try
            {
                sittingDate =
                    DateTime.ParseExact(
                        questionDocument.QuerySelector(".publish-date").TextContent.Replace("Sitting date:", "").Trim(),
                        "d MMM yyyy", new CultureInfo("en-NZ"));
            }
            catch (Exception e)
            {
                return;
            }

            sittingDate = TimeZoneInfo.ConvertTimeToUtc(sittingDate, nzst);

            var currentParliament = _db.Parliaments.FirstOrDefault(p =>
                p.StartDate <= sittingDate && (p.EndDate == null || p.EndDate >= sittingDate));

            if (currentParliament == null)
            {
                throw new Exception("Sitting date has no session: " + sittingDate);
            }

            var debates = questionDocument.QuerySelectorAll(".hansard__level li");
            var hansard = questionDocument.QuerySelector(".Hansard");

            // If the debate nodes aren't null, then we have the newer Hansard format
            if (debates != null && debates.Length != 0)
            {
                await ProcessHansardV2(sittingDate, debates, currentParliament);
            }
            else if (hansard != null)
            {
                await ProcessHansardV1(sittingDate, hansard, currentParliament);
            }
            else
            {
                throw new Exception("No hansard or speech element found!");
            }
        }

        public async Task GetByParliament(int parliament)
        {
            await GetUrls(
                "https://www.parliament.nz/en/pb/hansard-debates/rhr/?criteria.ParliamentNumber=" + parliament);
        }

        public async Task GetDifferential()
        {
            TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

            var lastDate = _db.Questions.OrderByDescending(q => q.Timestamp).First().Timestamp;

            // We want to make sure we aren't grabbing stuff that isn't quite done yet, so we'll do a 2 day delay on the import
            var importDate = DateTime.UtcNow.AddDays(-2);

            if (importDate > lastDate)
            {
                lastDate = TimeZoneInfo.ConvertTimeFromUtc(lastDate, nzst);
                importDate = TimeZoneInfo.ConvertTimeFromUtc(importDate, nzst);

                await GetUrls(
                    "https://www.parliament.nz/en/ajax/hansardlisting/read/6227?criteria.ParliamentNumber=-1&criteria.DateFrom=" +
                    lastDate.ToString("yyyy-MM-dd") + "&criteria.DateTo=" + lastDate.ToString("yyyy-MM-dd"));
            }
        }

        private async Task GetUrls(string baseUrl)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document =
                await context.OpenAsync(baseUrl);

            var number = int.Parse(document.QuerySelector(".listing-result").ChildNodes.OfType<IText>()
                                       .Select(m => m.Text).FirstOrDefault().Split(" ").LastOrDefault() ??
                                   throw new Exception("Null page number"));
            var pages = Math.Ceiling(Convert.ToDouble(number) / 20.0);

            for (var i = 1; i <= pages; i++)
            {
                if (i != 1)
                {
                    document = await context.OpenAsync(
                        baseUrl + "&Criteria.PageNumber=" + i);
                }

                var urls = document.QuerySelectorAll(".hansard__heading a");

                foreach (var url in urls)
                {
                    if (url.Attributes["title"].Value.Contains("Volume"))
                        await ImportFromHansard("https://www.parliament.nz" + url.Attributes["href"].Value);
                }
            }
        }

        /// <summary>
        /// Process the newest version of Hansard Reports, in use since the 14th of July 2016.
        /// </summary>
        /// <param name="sittingDate">The date that was detected as the time parliament was sitting</param>
        /// <param name="speeches">A collection of nodes that contain the individual speeches</param>
        /// <param name="currentParliament">The parliament that the Hansard we are processing belongs too</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ProcessHansardV2(DateTime sittingDate, IHtmlCollection<IElement> speeches,
            Parliament currentParliament)
        {
            string lastQuestionTitle = null;
            string lastQuestionSubtitle = null;
            string lastQuestionDescription = null;
            string lastClause = null;
            Member lastMember = null;

            string lastQuestionCommitteeTitle = null;

            DateTime lastDate = sittingDate;
            TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

            Stage? lastStage = null;

            var newQuestions = new List<Question>();

            foreach (var speech in speeches)
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
                            var time = timeNode.Attributes["name"].Value.Replace("time_", "");
                            try
                            {
                                // Parse the date
                                DateTime speechTime = DateTime.ParseExact(time, "yyyyMMdd HH:mm:ss",
                                    new CultureInfo("en-NZ"));
                                // Convert the date to UTC
                                lastDate = TimeZoneInfo.ConvertTimeToUtc(speechTime, nzst);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }

                    // If the line contains the title of the debate
                    QuestionType lastQuestionType;
                    if (speechLine.ClassList.Any(c =>
                        c == "Debate" || c == "BillDebate" || c == "BillDebate2" || c == "Debatealone"))
                    {
                        // Remove excess whitespace and double spaces
                        var newQuestionTitle = speechLine.TextContent.Trim().Replace("  ", " ");

                        // Set the question title to title case
                        newQuestionTitle = new CultureInfo("en-NZ").TextInfo.ToTitleCase(newQuestionTitle.ToLower());

                        // If the question title has changed, then we have moved to a different item of business
                        if (lastQuestionTitle == null || !string.Equals(newQuestionTitle, lastQuestionTitle,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            lastQuestionSubtitle = null;
                            lastStage = null;
                            lastQuestionTitle = newQuestionTitle;
                        }
                    }

                    // If the line contains the subtitle of the debate
                    if (speechLine.ClassList.Any(c => c == "SubDebate" || c == "SubDebatealone"))
                    {
                        lastQuestionSubtitle =
                            speechLine.TextContent.Replace(" (continued)", "").Replace("  ", " ").Trim();

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
                        lastQuestionCommitteeTitle = speechLine.TextContent.Replace(" (continued)", "")
                            .Replace("  ", " ").Trim();
                    }

                    string speechContent = speechLine.TextContent;
                    // Look for any relevant motions
                    if ((speechContent.Contains("I move") || speechContent.Contains("The question now is") ||
                         speechContent.Contains("The question was put that")) &&
                        speechLine.ClassList.Any(c => c == "Speech" || c == "ContinueSpeech" || c == "a"))
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

                        var memberNameElements = speechLine.QuerySelectorAll("strong");
                        if (memberNameElements.Length > 0)
                        {
                            var memberNameAndTitle = string.Join("", memberNameElements.Select(m => m.TextContent));
                            var memberMoving = memberNameAndTitle.Split(" (")[0].ToLower();

                            if (memberMoving.ToLower().Contains("chairperson"))
                            {
                                memberMoving = memberNameAndTitle.ToLower().Replace("the chairperson (", "")
                                    .Replace("the temporary chairperson (", "")
                                    .Replace("chairperson (", "").Replace(")", "").Trim();
                            }

                            memberMoving = memberMoving.Replace("rt hon ", "").Replace("hon ", "").Replace("dr ", "")
                                .Replace("sir ", "").Replace("dame ", "").Replace("vui ", "")
                                .Replace("’", "'")
                                .Trim();

                            if (!memberMoving.ToLower().Contains("speaker"))
                            {
                                lastMember = _db.Members.FirstOrDefault(m =>
                                    (m.FirstName + " " + m.LastName).ToLower() == memberMoving.ToLower() ||
                                    m.AlsoKnownAs.ToLower() == memberMoving.ToLower());

                                if (lastMember == null)
                                    throw new Exception("Mover not found in DB, member name: " + memberMoving);
                            }
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
                            if ((speechContent.Contains("I move") || speechContent.Contains("The question now is") ||
                                 speechContent.Contains("The question was put that")) &&
                                speechLine.PreviousElementSibling.ClassList.Any(c =>
                                    c == "Speech" || c == "ContinueSpeech" || c == "a"))
                                lastClause = speechLine.InnerHtml;
                        }
                    }

                    // Look for any voice votes
                    if (speechLine.ClassList.Any(c =>
                        c == "IndentMarginalone" || c == "Speech" || c == "MarginHeading"))
                    {
                        string voiceVoteContent = speechLine.TextContent.Trim();

                        string voiceVoteHtml = speechLine.InnerHtml.Trim();

                        if (voiceVoteHtml.StartsWith("<strong>"))
                        {
                            voiceVoteHtml = voiceVoteHtml.Replace("<strong>", "").Replace("</strong>", "").Trim();
                        }

                        // If this line contains 'agreed to' or 'Bill read ... time' then it is likely a voice vote and if there is no extra HTML on the inside
                        if ((voiceVoteContent.EndsWith(" agreed to.") || (voiceVoteContent.StartsWith("Bill read ") &&
                                                                          voiceVoteContent.EndsWith(" time."))) &&
                            voiceVoteHtml == voiceVoteContent)
                        {
                            //// Specific clause for committees, when integrated into motion
                            //var clauseTitle = voiceVoteNode.QuerySelector("strong");

                            //if (clauseTitle != null)
                            //    lastQuestionSubtitle = clauseTitle.TextContent.Replace("  ", " ").Trim();

                            // The previous node might have a better motion description
                            var previousNode = speechLine.PreviousElementSibling;

                            // If the previous node exists, but its a clause for an amendment, then we save that clause
                            if (previousNode != null && previousNode.ClassList.Contains("clause"))
                            {
                                lastClause = previousNode.InnerHtml;
                                previousNode = previousNode.PreviousElementSibling;
                            }

                            // The next node might be a party vote
                            var nextNode = speechLine.NextElementSibling;

                            // If the nextNode node exists, but its a clause for an amendment, then we save that clause
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
                                lastQuestionDescription = previousNode.TextContent
                                    .Replace("The question was put that ", "That ").Trim();
                                // Remove the period from the end
                                lastQuestionDescription =
                                    lastQuestionDescription.Substring(0, lastQuestionDescription.Length - 1);
                            }
                            else if (voiceVoteContent.Contains("The question was put that "))
                            {
                                lastQuestionDescription =
                                    voiceVoteContent.Replace("The question was put that ", "That ").Trim();
                                // Remove the period from the end
                                lastQuestionDescription =
                                    lastQuestionDescription.Substring(0, lastQuestionDescription.Length - 1);
                            }
                            else if (lastQuestionDescription == null)
                            {
                                // As a last resort, use the result of the vote as a description, but remove the agreement information
                                lastQuestionDescription = voiceVoteContent.Replace(" not agreed to.", "")
                                    .Replace(" agreed to.", "").Trim();
                            }

                            if ((previousNode != null && previousNode.ClassList.Contains("VoteText") &&
                                 (lastQuestionDescription.Contains("read") ||
                                  lastQuestionDescription.Contains("agreed"))) || (nextNode != null &&
                                nextNode.ClassList.Contains("VoteReason") &&
                                voiceVoteContent.Contains("The question was put that ")))
                            {
                                // Remove the last question description and clause, as there should only be one question for each of these
                                lastQuestionDescription = null;
                                lastMember = null;
                                lastClause = null;
                                // This is actually the result of a party vote that has been miscategorised, so skip it
                                continue;
                            }

                            // Extract various information about the question from the description and subtitle
                            var extractionResults =
                                GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle, lastQuestionDescription);
                            lastQuestionType = extractionResults.Item2;
                            lastStage = extractionResults.Item1 ?? lastStage;
                            lastQuestionTitle = extractionResults.Item3 ?? lastQuestionTitle;

                            // Create the question
                            Question question = new Question();
                            newQuestions.Add(question);

                            // If the motion is simply about votes, then provide more detail
                            if (lastQuestionDescription == "Votes" && lastQuestionCommitteeTitle != null)
                            {
                                lastQuestionDescription = lastQuestionCommitteeTitle;
                            }

                            // For an amendment, see if there is a member
                            Member amendMember = null;
                            var memberNameMatch = lastQuestionDescription.Replace(" the the ", " ").Replace(" the ", "")
                                .Replace(" the Rt Hon ", " ").Replace(" the Hon ", " ").Replace(" Rt Hon ", " ")
                                .Replace(" Hon ", " ").Replace(" Dr ", " ").Replace(" Sir ", " ").Replace(" Dame ", " ")
                                .Replace(" Vui ", " ")
                                .Split(" in the name of ");

                            if (memberNameMatch.Length > 1)
                            {
                                var nameStart = memberNameMatch[1].Trim().ToLower().Replace("’", "'");
                                amendMember = _db.Members.FirstOrDefault(m =>
                                    nameStart.StartsWith((m.FirstName + " " + m.LastName).ToLower()) ||
                                    nameStart.StartsWith(m.AlsoKnownAs.ToLower()));

                                if (amendMember == null)
                                    throw new Exception("Mover not found in DB, member name: " + nameStart);
                            }

                            // Update the question as necessary
                            question.Title = lastQuestionTitle;
                            question.Subtitle = lastQuestionSubtitle;
                            question.Description = lastQuestionDescription;
                            question.Timestamp = lastDate;
                            question.Stage = lastStage;
                            question.QuestionType = lastQuestionType;
                            question.Clause = lastClause;
                            question.Member = amendMember ?? lastMember;
                            question.Parliament = currentParliament;

                            // Find if there is a bill associated with this question
                            if (question.Title.Contains(" Bill"))
                            {
                                var bill = _db.Bills.FirstOrDefault(b => b.Title.ToLower() == question.Title.ToLower());

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

                                var sop = _db.SupplementaryOrderPapers.FirstOrDefault(s => s.Number == sopNumber);

                                if (sop != null)
                                {
                                    question.SupplementaryOrderPaper = sop;
                                    question.Member = sop.Member;
                                }
                            }

                            // See if this voice vote already exists
                            VoiceVote vote = _db.VoiceVotes.FirstOrDefault(v => v.Question_Id == question.Id);

                            // If the vote doesn't exist, then we need to create a new one
                            if (vote == null)
                            {
                                vote = new VoiceVote();
                                await _db.VoiceVotes.AddAsync(vote);
                            }

                            // Update the vote
                            vote.Question = question;
                            vote.Position = !voiceVoteContent.Contains(" not agreed to");

                            // Remove the last question description and clause, as there should only be one question for each of these
                            lastQuestionDescription = null;
                            if (amendMember == null)
                                lastMember = null;
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

                        if (motion != null && (EmptyMotions.All(m =>
                                                   !string.Equals(m, motion,
                                                       StringComparison.CurrentCultureIgnoreCase)) ||
                                               lastQuestionDescription == null))
                            lastQuestionDescription = motion;

                        // The previous node might have a better motion description
                        var previousNode = speechLine.PreviousElementSibling;

                        // If the previous node exists, but its a schedule for an amendment, then we need to go back
                        if (previousNode != null && previousNode.ClassList.Any(c => c == "table" || c == "tablescroll"))
                        {
                            lastClause = previousNode.InnerHtml;
                            previousNode = previousNode.PreviousElementSibling;
                        }

                        // If the previous node exists, but its a clause for an amendment, then we simply save that clause
                        if (previousNode != null && previousNode.ClassList.Contains("clause"))
                        {
                            lastClause = previousNode.InnerHtml + (lastClause ?? "");
                            previousNode = previousNode.PreviousElementSibling;
                        }

                        if (previousNode != null && previousNode.TextContent.Contains("The question was put that "))
                        {
                            lastQuestionDescription = previousNode.TextContent
                                .Replace("The question was put that ", "That ").Trim();
                            // Remove the period from the end
                            lastQuestionDescription =
                                lastQuestionDescription.Substring(0, lastQuestionDescription.Length - 1);
                        }

                        // Extract various information about the question from the description and subtitle
                        var extractionResults =
                            GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle, lastQuestionDescription);
                        lastQuestionType = extractionResults.Item2;
                        lastStage = extractionResults.Item1 ?? lastStage;
                        lastQuestionTitle = extractionResults.Item3 ?? lastQuestionTitle;

                        // For an amendment, see if there is a member
                        Member amendMember = null;
                        var memberNameMatch = lastQuestionDescription.Replace(" the the ", " ").Replace(" the ", "")
                            .Replace(" the Rt Hon ", " ").Replace(" the Hon ", " ").Replace(" Rt Hon ", " ")
                            .Replace(" Hon ", " ").Replace(" Dr ", " ").Replace(" Sir ", " ").Replace(" Dame ", " ")
                            .Replace(" Vui ", " ")
                            .Split(" in the name of ");

                        if (memberNameMatch.Length > 1)
                        {
                            var nameStart = memberNameMatch[1].Trim().Replace("’", "'").ToLower();
                            amendMember = _db.Members.FirstOrDefault(m =>
                                nameStart.StartsWith((m.FirstName + " " + m.LastName).ToLower()) ||
                                nameStart.StartsWith(m.AlsoKnownAs.ToLower()));

                            if (amendMember == null)
                                throw new Exception("Mover not found in DB, member name: " + nameStart);
                        }

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
                        question.Member = amendMember ?? lastMember;
                        question.Parliament = currentParliament;

                        // Find if there is a bill associated with this question
                        if (question.Title.Contains(" Bill"))
                        {
                            var bill = _db.Bills.FirstOrDefault(b => b.Title.ToLower() == question.Title.ToLower());

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

                            var sop = _db.SupplementaryOrderPapers.FirstOrDefault(s => s.Number == sopNumber);

                            if (sop != null)
                            {
                                question.SupplementaryOrderPaper = sop;
                                question.Member = sop.Member;
                            }
                        }

                        // Remove any existing party votes for this question
                        var existingPartyVotes = _db.PartyVotes.Where(p => p.Question_Id == question.Id);
                        _db.PartyVotes.RemoveRange(existingPartyVotes);

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
                                var otherVotesMatch = Regex.Match(votersText,
                                    @"Other [0-9]+:{0,1} \({0,1}([^).]+)\){0,1}");
                                if (otherVotesMatch.Success)
                                {
                                    // We're going to treat them as independents for the purposes of this party vote, so we need to convert their names to the correct format
                                    string otherVoters = otherVotesMatch.Groups[1].Value.Replace(", ", "; ");

                                    // Replace the 'others' section with the correct voters
                                    votersText = votersText.Replace(otherVotesMatch.Groups[0].Value, otherVoters);
                                }

                                // Weird fix for some ACT votes
                                votersText = votersText.Replace(", ACT ", "; ACT ").Replace(". ACT ", "; ACT ");

                                // Remove the 'Other' provision and treat MPs like independents
                                var otherRegex = new Regex(@"Other \(.*?\)");
                                votersText = otherRegex.Replace(votersText,
                                    m => m.Value.Replace("Other (", "").Replace(")", "")
                                        .Replace(",", ";"));

                                string[] voters = votersText.Replace(".", "").Replace(":", ";").Split(";");

                                foreach (string voter in voters)
                                {
                                    // Parse out the name of the party and how many voted in it
                                    var partyMatch = Regex.Match(voter, @"(.+) ([0-9]+)");

                                    if (partyMatch.Success)
                                    {
                                        // Get the party name and the number of voters
                                        string partyName = partyMatch.Groups[1].Value.Trim().Replace("’", "'");
                                        int partyNumbers = int.Parse(partyMatch.Groups[2].Value);

                                        // Find the party in the database
                                        var party = _db.Parties.FirstOrDefault(p =>
                                            p.Name == partyName || p.AlsoKnownAs.Contains(partyName));

                                        Member member = null;
                                        // If the party doesn't exist, look for a particular member
                                        if (party == null)
                                            member = _db.Members.FirstOrDefault(m =>
                                                (m.LastName == partyName ||
                                                 m.LastName + " " + m.FirstName.Substring(0, 1) == partyName ||
                                                 m.FirstName + " " + m.LastName == partyName ||
                                                 m.AlsoKnownAs.Contains(partyName)) && m.Tenures.Any(t =>
                                                    t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        if (party == null && member == null)
                                            throw new Exception("Party not found in DB. Party name: " + partyName);

                                        // Create a new party vote and add to DB
                                        var partyVote = new PartyVote();
                                        await _db.PartyVotes.AddAsync(partyVote);

                                        // See if this vote is a split party vote
                                        var splitPartyVoteMatch = Regex.Match(voter, @"(.+) ([0-9]+) \((.+)\)");

                                        // If it is, then we need to go through the different members and record their positions
                                        if (splitPartyVoteMatch.Success)
                                        {
                                            // This is a split party vote, and needs to be treated accordingly
                                            var members = splitPartyVoteMatch.Groups[3].Value.Replace("’", "'")
                                                .Split(", ");
                                            List<Member> splitMembers = _db.Members.Where(m =>
                                                    (members.Contains(m.LastName) ||
                                                     members.Contains(m.LastName + " " + m.FirstName.Substring(0, 1))
                                                    ) &&
                                                    m.Tenures.Any(t =>
                                                        t.Start <= sittingDate &&
                                                        (t.End == null || t.End >= sittingDate)))
                                                .ToList();

                                            await _db.SplitPartyVotes.AddRangeAsync(splitMembers.Select(m =>
                                                new SplitPartyVote(partyVote, m)));
                                        }

                                        // Update the party vote accordingly
                                        partyVote.Update(question, partyNumbers, lastPosition, party,
                                            complexPosition: lastComplexPosition, member: member);
                                    }
                                    // This is for a independent MP, so we need to add them individually
                                    else
                                    {
                                        // Parse out the member name if it has a prefix
                                        var memberName = voter.Replace("Independent: ", "").Replace("Independent ", "")
                                            .Replace("’", "'").Trim();

                                        // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                        var member = _db.Members.FirstOrDefault(m =>
                                            (m.LastName == memberName ||
                                             m.LastName + " " + m.FirstName.Substring(0, 1) == memberName ||
                                             m.FirstName + " " + m.LastName == memberName ||
                                             m.AlsoKnownAs.Contains(memberName)) && m.Tenures.Any(t =>
                                                t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        Party party = null;
                                        // If the member doesn't exist, then it may actually be a party vote
                                        if (member == null)
                                        {
                                            // Find the party in the database
                                            party = _db.Parties.FirstOrDefault(p =>
                                                p.Name == memberName || p.AlsoKnownAs.Contains(memberName));
                                        }

                                        if (party == null && member == null)
                                            throw new Exception("Member not found in DB. Member name " + memberName);

                                        // Add the new party vote to the DB
                                        var partyVote = new PartyVote();
                                        await _db.PartyVotes.AddAsync(partyVote);

                                        // Update the party vote
                                        partyVote.Update(question, 1, party: party, member: member,
                                            position: lastPosition, complexPosition: lastComplexPosition);
                                    }
                                }
                            }

                            else if (voteNode.ClassList.Contains("tablescroll") || voteNode.ClassList.Contains("table"))
                            {
                                var memberNodes = voteNode.QuerySelectorAll("td");

                                foreach (var memberNode in memberNodes)
                                {
                                    // Get the member name, determine if they voted by proxy and then remove the ' (P)' and 'Teller:' if necessary
                                    var memberName = memberNode.TextContent.Replace("Teller:", "").Trim();
                                    if (!string.IsNullOrEmpty(memberName))
                                    {
                                        var isProxy = memberName.Contains("(P)");
                                        memberName = memberName.Replace("(P)", "").Replace("’", "'").Trim();

                                        // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                        var member = _db.Members.FirstOrDefault(m =>
                                            (m.LastName == memberName ||
                                             m.LastName + " " + m.FirstName.Substring(0, 1) == memberName ||
                                             m.FirstName + " " + m.LastName == memberName ||
                                             m.AlsoKnownAs.Contains(memberName)) && m.Tenures.Any(t =>
                                                t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                        if (member == null)
                                            throw new Exception("Member not found in DB. Member name " + memberName);

                                        // Add the new personal vote to the DB
                                        PersonalVote personalVote = new PersonalVote();
                                        await _db.PersonalVotes.AddAsync(personalVote);

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
                        if (amendMember == null)
                            lastMember = null;
                        lastClause = null;
                    }
                }
            }

            await _db.Questions.AddRangeAsync(newQuestions);

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Process the older version of Hansard Reports that was in use prior to the 14th of July 2016.
        /// </summary>
        /// <param name="sittingDate"></param>
        /// <param name="hansard"></param>
        /// <param name="currentParliament"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ProcessHansardV1(DateTime sittingDate, IElement hansard, Parliament currentParliament)
        {
            string lastQuestionTitle = null;
            string lastQuestionSubtitle = null;
            string lastQuestionDescription = null;
            Member lastMember = null;

            string lastQuestionCommitteeTitle = null;

            DateTime lastDate = sittingDate;
            TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

            Stage? lastStage = null;
            QuestionType lastQuestionType = QuestionType.Motion;

            List<Question> newQuestions = new List<Question>();

            // Get all of the speeches for this day
            var debates = hansard.QuerySelectorAll(".Debate, .BillDebate, .BillDebate2, .DebateDebate");

            foreach (var debate in debates)
            {
                // Iterate through the child elements
                foreach (var child in debate.Children)
                {
                    // This is a title, so treat as such
                    if (child.TagName.StartsWith("H"))
                    {
                        string textContent = child.TextContent.Replace(" (continued)", "").Trim();

                        if (textContent != "Motions")
                        {
                            if (lastQuestionTitle == null)
                            {
                                lastQuestionTitle = textContent;

                                lastStage = null;
                                lastQuestionType = QuestionType.Motion;
                                lastQuestionSubtitle = null;
                                lastQuestionDescription = null;
                                lastMember = null;
                            }
                            else if (lastQuestionSubtitle == null)
                            {
                                lastQuestionSubtitle = textContent;
                            }
                        }
                    }

                    // This is where the actual debate information is stored
                    if (child.ClassList.Contains("SubDebate"))
                    {
                        // Iterate through each speech
                        foreach (var speech in child.Children)
                        {
                            // This is a title, so treat as such
                            if (speech.TagName.StartsWith("H") ||
                                (speech.Children.Length == 1 && speech.Children.First().TagName.StartsWith("H")))
                            {
                                // Stip out continued, to clean up the titles
                                string textContent = speech.TextContent.Replace(" (continued)", "")
                                    .Replace(" agreed to", "").Trim();

                                // Assuming the text content isn't simply 'Motions', in which case we ignore it
                                if (textContent != "Motions")
                                {
                                    // If there is no question title set one
                                    if (lastQuestionTitle == null)
                                    {
                                        lastQuestionTitle = textContent;

                                        lastStage = null;
                                        lastQuestionType = QuestionType.Motion;
                                        lastQuestionSubtitle = null;
                                        lastQuestionDescription = null;
                                        lastMember = null;
                                    }
                                    // If there is no subtitle also set one
                                    else if (lastQuestionSubtitle == null)
                                    {
                                        lastQuestionSubtitle = textContent;
                                    }
                                    // Finally, just set the committee title
                                    else
                                    {
                                        lastQuestionCommitteeTitle = textContent;
                                    }
                                }
                            }

                            // An actual speech
                            if (speech.ClassList.Contains("Speech"))
                            {
                                // Iterate through each line of the speech, looking for motions
                                foreach (var speechLine in speech.Children)
                                {
                                    // Look for the time of the latest speech

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
                                                DateTime speechTime = DateTime.ParseExact(time, "yyyyMMdd HH:mm:ss",
                                                    new CultureInfo("en-NZ"));
                                                // Convert the date to UTC
                                                lastDate = TimeZoneInfo.ConvertTimeToUtc(speechTime, nzst);
                                            }
                                            catch (Exception)
                                            {
                                                // ignored
                                            }
                                        }
                                    }

                                    string speechContent = speechLine.TextContent;

                                    // Look for any relevant motions
                                    if ((speechContent.Contains("I move") ||
                                         speechContent.Contains("The question now is") ||
                                         speechContent.Contains("The question was put that")) &&
                                        speechLine.ClassList.Any(
                                            c => c == "Speech" || c == "ContinueSpeech" || c == "a"))
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

                                        var memberNameElements = speechLine.QuerySelectorAll("strong");
                                        if (memberNameElements.Length > 0)
                                        {
                                            var memberNameAndTitle = string.Join("",
                                                memberNameElements.Select(m => m.TextContent));
                                            var memberMoving = memberNameAndTitle.Split(" (")[0].ToLower();

                                            if (memberMoving.ToLower().Contains("chairperson"))
                                            {
                                                memberMoving = memberNameAndTitle.ToLower()
                                                    .Replace("the chairperson (", "")
                                                    .Replace("the temporary chairperson (", "")
                                                    .Replace("chairperson (", "").Replace(")", "").Trim();
                                            }

                                            memberMoving = memberMoving.Replace("rt hon ", "").Replace("hon ", "")
                                                .Replace("dr ", "").Replace("sir ", "").Replace("dame ", "").Replace("vui ", "")
                                                .Replace("’", "'").Replace("\n", "")
                                                .Trim();

                                            if (!memberMoving.ToLower().Contains("speaker") &&
                                                !string.IsNullOrEmpty(memberMoving) && memberMoving != ",")
                                            {
                                                lastMember = _db.Members.FirstOrDefault(m =>
                                                    (m.FirstName + " " + m.LastName).ToLower() ==
                                                    memberMoving.ToLower() ||
                                                    m.AlsoKnownAs.ToLower() == memberMoving.ToLower());

                                                if (lastMember == null)
                                                    throw new Exception("Mover not found in DB, member name: " +
                                                                        memberMoving);
                                            }
                                        }
                                    }

                                    // The motion lapsed, so clear the motion
                                    if (speechContent.ToLower().Contains("motion lapsed"))
                                    {
                                        lastQuestionDescription = null;
                                        lastMember = null;
                                    }

                                    if (speechLine.TagName == "UL" || speechLine.ClassList.Contains("section"))
                                    {
                                        string voiceVoteContent = speechLine.TextContent.Trim();

                                        if ((voiceVoteContent.EndsWith("agreed to.") ||
                                             (voiceVoteContent.StartsWith("Bill read ") &&
                                              voiceVoteContent.EndsWith(" time."))) &&
                                            !voiceVoteContent.Contains("The question was put that "))
                                        {
                                            var result = ProcessVoiceVote(speechLine, voiceVoteContent,
                                                lastQuestionTitle, lastQuestionSubtitle, lastQuestionCommitteeTitle,
                                                lastQuestionDescription, lastStage, lastDate, currentParliament,
                                                lastMember);

                                            newQuestions.Add(result.Item1);

                                            // Remove the last question description and clause, as there should only be one question for each of these
                                            lastQuestionDescription = null;
                                            if (result.Item2 == null)
                                                lastMember = null;
                                            lastQuestionType = QuestionType.Motion;
                                        }
                                    }
                                }
                            }

                            if (speech.TagName == "UL" || speech.ClassList.Contains("section"))
                            {
                                string voiceVoteContent = speech.TextContent.Trim();

                                if ((voiceVoteContent.EndsWith("agreed to.") ||
                                     (voiceVoteContent.StartsWith("Bill read ") &&
                                      voiceVoteContent.EndsWith(" time."))) &&
                                    !voiceVoteContent.Contains("The question was put that "))
                                {
                                    var result = ProcessVoiceVote(speech, voiceVoteContent,
                                        lastQuestionTitle, lastQuestionSubtitle, lastQuestionCommitteeTitle,
                                        lastQuestionDescription, lastStage, lastDate, currentParliament,
                                        lastMember);

                                    newQuestions.Add(result.Item1);

                                    // Remove the last question description and clause, as there should only be one question for each of these
                                    lastQuestionDescription = null;
                                    if (result.Item2 == null)
                                        lastMember = null;
                                    lastQuestionType = QuestionType.Motion;
                                }
                            }

                            // Look for any party votes
                            if (speech.ClassList.Contains("partyVote"))
                            {
                                foreach (var partyVoteComponent in speech.Children)
                                {
                                    if (partyVoteComponent.ClassList.Contains("vote"))
                                    {
                                        Question question = null;

                                        // Iterate through the table components
                                        foreach (var tableComponent in partyVoteComponent.Children)
                                        {
                                            if (tableComponent.TagName == "CAPTION")
                                            {
                                                var questionDescriptionNodes =
                                                    tableComponent.QuerySelectorAll("em");

                                                string motion = null;

                                                if (questionDescriptionNodes != null &&
                                                    questionDescriptionNodes.Length != 0)
                                                    motion = string.Join("",
                                                            questionDescriptionNodes.Select(q => q.TextContent))
                                                        .Trim();
                                                else
                                                    motion = tableComponent.TextContent
                                                        .Replace("A party vote was called for on the question, ", "")
                                                        .Trim();

                                                if (motion != null && (EmptyMotions.All(m =>
                                                                           !string.Equals(m, motion,
                                                                               StringComparison
                                                                                   .CurrentCultureIgnoreCase)) ||
                                                                       lastQuestionDescription == null))
                                                    lastQuestionDescription = motion;

                                                // The previous node might have a better motion description
                                                var previousNode = speech.PreviousElementSibling;

                                                if (previousNode != null)
                                                {
                                                    // If the last node was declaration of a party vote, then use that as the motion
                                                    if (previousNode.TagName == "UL" &&
                                                        previousNode.TextContent.Contains(
                                                            "The question was put that "))
                                                    {
                                                        lastQuestionDescription = previousNode.TextContent
                                                            .Replace("The question was put that ", "That ")
                                                            .Trim();
                                                        // Remove the period from the end
                                                        lastQuestionDescription =
                                                            lastQuestionDescription.Substring(0,
                                                                lastQuestionDescription.Length - 1);
                                                    }

                                                    // If its a speech, then we need to find the last bullet item in the list
                                                    else if (previousNode.ClassList.Contains("Speech") ||
                                                             previousNode.ClassList.Contains("partyVote"))
                                                    {
                                                        previousNode =
                                                            previousNode.Children.LastOrDefault(c =>
                                                                c.TagName == "UL");

                                                        if (previousNode != null &&
                                                            previousNode.TextContent.Contains(
                                                                "The question was put that "))
                                                        {
                                                            lastQuestionDescription = previousNode.TextContent
                                                                .Replace("The question was put that ", "That ")
                                                                .Trim();
                                                            // Remove the period from the end
                                                            lastQuestionDescription =
                                                                lastQuestionDescription.Substring(0,
                                                                    lastQuestionDescription.Length - 1);
                                                        }
                                                    }
                                                }

                                                if (lastQuestionDescription == null)
                                                {
                                                    int i = 0;
                                                }

                                                // Extract various information about the question from the description and subtitle
                                                var extractionResults =
                                                    GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle,
                                                        lastQuestionDescription);
                                                lastQuestionType = extractionResults.Item2;
                                                lastStage = extractionResults.Item1 ?? lastStage;
                                                lastQuestionTitle =
                                                    extractionResults.Item3 ?? lastQuestionTitle;

                                                // For an amendment, see if there is a member
                                                Member amendMember = null;
                                                var memberNameMatch = lastQuestionDescription
                                                    .Replace(" the the ", " ").Replace(" the ", "")
                                                    .Replace(" the Rt Hon ", " ").Replace(" the Hon ", " ")
                                                    .Replace(" Rt Hon ", " ")
                                                    .Replace(" Sir ", " ")
                                                    .Replace(" Dame ", " ")
                                                    .Replace(" Hon ", " ").Replace(" Dr ", " ")
                                                    .Replace(" Vui ", " ")
                                                    .Split(" in the name of ");

                                                if (memberNameMatch.Length > 1)
                                                {
                                                    var nameStart = memberNameMatch[1].Trim().ToLower()
                                                        .Replace("’", "'");
                                                    amendMember = _db.Members.FirstOrDefault(m =>
                                                        nameStart.StartsWith((m.FirstName + " " + m.LastName)
                                                            .ToLower()) ||
                                                        nameStart.StartsWith(m.AlsoKnownAs.ToLower()));

                                                    if (amendMember == null)
                                                        throw new Exception(
                                                            "Mover not found in DB, member name: " + nameStart);
                                                }

                                                // Create a new question
                                                question = new Question();
                                                newQuestions.Add(question);

                                                // Update the question as necessary
                                                question.Title = lastQuestionTitle;
                                                question.Subtitle = lastQuestionSubtitle;
                                                question.Description = lastQuestionDescription;
                                                question.Timestamp = lastDate;
                                                question.Stage = lastStage;
                                                question.QuestionType = lastQuestionType;
                                                question.Member = amendMember ?? lastMember;
                                                question.Parliament = currentParliament;

                                                // Find if there is a bill associated with this question
                                                if (question.Title.Contains(" Bill"))
                                                {
                                                    var bill = _db.Bills.FirstOrDefault(b =>
                                                        b.Title.ToLower() == question.Title.ToLower());

                                                    if (bill != null)
                                                    {
                                                        question.Title = bill.Title;
                                                        question.Bill = bill;
                                                    }
                                                }

                                                // Find if there is a SOP associated with this question
                                                Match sopMatch = Regex.Match(question.Description,
                                                    @"Supplementary Order Paper ([0-9]+)");
                                                if (sopMatch.Success &&
                                                    !question.Description.Contains(" amendment "))
                                                {
                                                    var sopNumber = int.Parse(sopMatch.Groups[1].Value);

                                                    var sop = _db.SupplementaryOrderPapers.FirstOrDefault(s =>
                                                        s.Number == sopNumber);

                                                    if (sop != null)
                                                    {
                                                        question.SupplementaryOrderPaper = sop;
                                                        question.Member = sop.Member;
                                                    }
                                                }

                                                // Clear the description and clause
                                                lastQuestionDescription = null;
                                                if (amendMember == null)
                                                    lastMember = null;

                                                // Remove any existing party votes for this question
                                                var existingPartyVotes =
                                                    _db.PartyVotes.Where(p => p.Question_Id == question.Id);
                                                _db.PartyVotes.RemoveRange(existingPartyVotes);
                                            }

                                            // The actual body of the table
                                            else if (tableComponent.TagName == "TBODY" && question != null)
                                            {
                                                // Iterate through each row of the party vote body
                                                foreach (var partyVoteRow in tableComponent.Children)
                                                {
                                                    bool? lastPosition = null;
                                                    string lastComplexPosition = null;

                                                    var voteCountNode =
                                                        partyVoteRow.QuerySelector(".VoteCount");

                                                    if (voteCountNode != null)
                                                    {
                                                        // Parse out the actual position, ignoring vote totals (as we calculate these ourselves)
                                                        string positionText = voteCountNode.TextContent.Trim();
                                                        Match positionMatch = Regex.Match(positionText,
                                                            @"(.+)[ \n]+([0-9]+)");
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

                                                    var voteTextNode = partyVoteRow.QuerySelector(".VoteText");

                                                    if (voteTextNode != null)
                                                    {
                                                        string votersText = voteTextNode.TextContent.Trim();

                                                        // Reformat 'any other votes'
                                                        var otherVotesMatch = Regex.Match(votersText,
                                                            @"Other [0-9]+:{0,1} \({0,1}([^).]+)\){0,1}");
                                                        if (otherVotesMatch.Success)
                                                        {
                                                            // We're going to treat them as independents for the purposes of this party vote, so we need to convert their names to the correct format
                                                            string otherVoters = otherVotesMatch.Groups[1].Value
                                                                .Replace(", ", "; ");

                                                            // Replace the 'others' section with the correct voters
                                                            votersText =
                                                                votersText.Replace(
                                                                    otherVotesMatch.Groups[0].Value,
                                                                    otherVoters);
                                                        }

                                                        // Remove the 'Other' provision and treat MPs like independents
                                                        var otherRegex = new Regex(@"Other \(.*?\)");
                                                        votersText = otherRegex.Replace(votersText,
                                                            m => m.Value.Replace("Other (", "").Replace(")", "")
                                                                .Replace(",", ";")).Replace("Other: ", "");
                                                        
                                                        // United independent typo
                                                        votersText = votersText.Replace("United Independent: ",
                                                            "Independent: ");

                                                        // Remove the 'independents' bit
                                                        var independentsRegex = new Regex(@"Independents: ([^;.]+)");
                                                        votersText = independentsRegex.Replace(votersText,
                                                            m => m.Value.Replace("Independents: ", "")
                                                                .Replace(",", ";"));

                                                        var independentRegex = new Regex(@"Independent: ([^;.]+)");
                                                        votersText = independentRegex.Replace(votersText,
                                                            m => m.Value.Replace("Independent: ", "")
                                                                .Replace(",", ";"));

                                                        // Fix for united future bug
                                                        votersText = votersText.Replace(", United Future",
                                                            "; United Future");

                                                        // Fix for mana bug
                                                        votersText = votersText.Replace(", Mana", "; Mana")
                                                            .Replace("Mana: ", "Mana ");

                                                        // Fix for progressive bug
                                                        votersText = votersText.Replace("Progressive1",
                                                            "Progressive 1");
                                                        votersText = votersText.Replace("Progressive: 1",
                                                            "Progressive 1");
                                                        votersText = votersText.Replace("United Future 2 Progressive",
                                                            "United Future 2; Progressive");

                                                        // Fix for maori party bugs
                                                        votersText = votersText.Replace("Māori Party (Sharples) 1",
                                                            "Māori Party 1 (Sharples)");
                                                        votersText = votersText.Replace(
                                                            "Māori Party (Flavell, Katene, Turia) 3",
                                                            "Māori Party 3 (Flavell, Katene, Turia)");
                                                        votersText = votersText.Replace(", Māori Party",
                                                            "; Māori Party");
                                                        
                                                        // Fix for act bug
                                                        votersText = votersText.Replace(", ACT New Zealand",
                                                            "; ACT New Zealand");
                                                        votersText = votersText.Replace(
                                                            "ACT New Zealand (Boscawen, Garrett, Hide)3",
                                                            "ACT New Zealand 3 (Boscawen, Garrett, Hide)");
                                                        votersText = votersText.Replace(
                                                            "ACT New Zealand (Roy, Douglas) 2",
                                                            "ACT New Zealand 2 (Roy, Douglas)");
                                                        votersText = votersText.Replace("Turner) ACT New Zealand",
                                                            "Turner); ACT New Zealand");

                                                        string[] voters = votersText.Replace(".", "")
                                                            .Replace(":", ";").Split(";");

                                                        foreach (string voter in voters)
                                                        {
                                                            // Parse out the name of the party and how many voted in it
                                                            var partyMatch = Regex.Match(voter,
                                                                @"(.+) ([0-9]+)");

                                                            if (partyMatch.Success)
                                                            {
                                                                // Get the party name and the number of voters
                                                                string partyName = partyMatch.Groups[1].Value
                                                                    .Trim().Replace("’", "'");
                                                                int partyNumbers =
                                                                    int.Parse(partyMatch.Groups[2].Value);

                                                                // Find the party in the database
                                                                var party = _db.Parties.FirstOrDefault(p =>
                                                                    p.Name == partyName ||
                                                                    p.AlsoKnownAs.Contains(partyName));

                                                                Member member = null;
                                                                // If the party doesn't exist, look for a particular member
                                                                if (party == null)
                                                                    member = _db.Members.FirstOrDefault(m =>
                                                                        (m.LastName == partyName ||
                                                                         m.LastName + " " +
                                                                         m.FirstName.Substring(0, 1) ==
                                                                         partyName ||
                                                                         m.FirstName + " " + m.LastName ==
                                                                         partyName ||
                                                                         m.AlsoKnownAs.Contains(partyName)
                                                                        ) && m.Tenures.Any(t =>
                                                                            t.Start <= sittingDate &&
                                                                            (t.End == null ||
                                                                             t.End >= sittingDate)));

                                                                if (party == null && member == null)
                                                                    throw new Exception(
                                                                        "Party not found in DB. Party name: " +
                                                                        partyName);

                                                                // Create a new party vote and add to DB
                                                                var partyVote = new PartyVote();
                                                                await _db.PartyVotes.AddAsync(partyVote);

                                                                // See if this vote is a split party vote
                                                                var splitPartyVoteMatch = Regex.Match(voter,
                                                                    @"(.+) ([0-9]+) \((.+)\)");

                                                                // If it is, then we need to go through the different members and record their positions
                                                                if (splitPartyVoteMatch.Success)
                                                                {
                                                                    // This is a split party vote, and needs to be treated accordingly
                                                                    var members = splitPartyVoteMatch.Groups[3]
                                                                        .Value.Replace("’", "'").Split(", ");
                                                                    List<Member> splitMembers = _db.Members
                                                                        .Where(m =>
                                                                            (members.Contains(m.LastName) ||
                                                                             members.Contains(m.LastName +
                                                                                 " " + m.FirstName.Substring(
                                                                                     0, 1))
                                                                            ) &&
                                                                            m.Tenures.Any(t =>
                                                                                t.Start <= sittingDate &&
                                                                                (t.End == null ||
                                                                                    t.End >= sittingDate)))
                                                                        .ToList();

                                                                    await _db.SplitPartyVotes.AddRangeAsync(
                                                                        splitMembers.Select(m =>
                                                                            new SplitPartyVote(partyVote, m)));
                                                                }

                                                                // Update the party vote accordingly
                                                                partyVote.Update(question, partyNumbers,
                                                                    lastPosition, party,
                                                                    complexPosition: lastComplexPosition,
                                                                    member: member);
                                                            }
                                                            // This is for a independent MP, so we need to add them individually
                                                            else
                                                            {
                                                                // Parse out the member name if it has a prefix
                                                                var memberName =
                                                                    voter.Replace("Independent: ", "")
                                                                        .Replace("Independent ", "").Replace("’", "'")
                                                                        .Trim();

                                                                // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                                                var member = _db.Members.FirstOrDefault(m =>
                                                                    (m.LastName == memberName ||
                                                                     m.LastName + " " +
                                                                     m.FirstName.Substring(0, 1) ==
                                                                     memberName ||
                                                                     m.FirstName + " " + m.LastName ==
                                                                     memberName ||
                                                                     m.AlsoKnownAs.Contains(memberName)) &&
                                                                    m.Tenures.Any(t =>
                                                                        t.Start <= sittingDate &&
                                                                        (t.End == null ||
                                                                         t.End >= sittingDate)));

                                                                Party party = null;
                                                                // If the member doesn't exist, then it may actually be a party vote
                                                                if (member == null)
                                                                {
                                                                    // Find the party in the database
                                                                    party = _db.Parties.FirstOrDefault(p =>
                                                                        p.Name == memberName ||
                                                                        p.AlsoKnownAs.Contains(memberName));
                                                                }

                                                                if (party == null && member == null)
                                                                    throw new Exception(
                                                                        "Member not found in DB. Member name " +
                                                                        memberName);

                                                                // Add the new party vote to the DB
                                                                var partyVote = new PartyVote();
                                                                await _db.PartyVotes.AddAsync(partyVote);

                                                                // Update the party vote
                                                                partyVote.Update(question, 1, party: party,
                                                                    member: member,
                                                                    position: lastPosition,
                                                                    complexPosition: lastComplexPosition);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // If the party vote section also contains a voice vote
                                    if (partyVoteComponent.TagName == "UL" ||
                                        partyVoteComponent.ClassList.Contains("section"))
                                    {
                                        string voiceVoteContent = partyVoteComponent.TextContent.Trim();

                                        if ((voiceVoteContent.EndsWith("agreed to.") ||
                                             (voiceVoteContent.StartsWith("Bill read ") &&
                                              voiceVoteContent.EndsWith(" time."))) &&
                                            !voiceVoteContent.Contains("The question was put that "))
                                        {
                                            var result = ProcessVoiceVote(partyVoteComponent, voiceVoteContent,
                                                lastQuestionTitle, lastQuestionSubtitle,
                                                lastQuestionCommitteeTitle,
                                                lastQuestionDescription, lastStage, lastDate, currentParliament,
                                                lastMember);

                                            newQuestions.Add(result.Item1);

                                            // Remove the last question description and clause, as there should only be one question for each of these
                                            lastQuestionDescription = null;
                                            if (result.Item2 == null)
                                                lastMember = null;
                                            lastQuestionType = QuestionType.Motion;
                                        }
                                    }
                                }
                            }

                            // Look for any personal votes
                            if (speech.ClassList.Contains("personalVote"))
                            {
                                // Find the nodes that contain the motion
                                var questionDescriptionNodes = speech.QuerySelectorAll("em");

                                string motion = null;

                                if (questionDescriptionNodes != null &&
                                    questionDescriptionNodes.Length != 0)
                                    motion = string.Join("",
                                            questionDescriptionNodes.Select(q => q.TextContent))
                                        .Trim();

                                if (motion != null && (EmptyMotions.All(m =>
                                                           !string.Equals(m, motion,
                                                               StringComparison
                                                                   .CurrentCultureIgnoreCase)) ||
                                                       lastQuestionDescription == null))
                                    lastQuestionDescription = motion;

                                if (lastQuestionDescription == null)
                                {
                                    var firstTextNode =
                                        speech.ChildNodes.FirstOrDefault(c => c.NodeType == NodeType.Text);

                                    if (firstTextNode != null &&
                                        firstTextNode.TextContent.Contains(
                                            "A personal vote was called for on the question"))
                                    {
                                        lastQuestionDescription = firstTextNode.TextContent
                                            .Replace("A personal vote was called for on the question, ", "").Trim();
                                    }
                                }

                                // The previous node might have a better motion description
                                var previousNode = speech.PreviousElementSibling;

                                if (previousNode != null)
                                {
                                    // If the last node was declaration of a party vote, then use that as the motion
                                    if (previousNode.TagName == "UL" &&
                                        previousNode.TextContent.Contains(
                                            "The question was put that "))
                                    {
                                        lastQuestionDescription = previousNode.TextContent
                                            .Replace("The question was put that ", "That ")
                                            .Trim();
                                        // Remove the period from the end
                                        lastQuestionDescription =
                                            lastQuestionDescription.Substring(0,
                                                lastQuestionDescription.Length - 1);
                                    }

                                    // If its a speech, then we need to find the last bullet item in the list
                                    else if (previousNode.ClassList.Contains("Speech"))
                                    {
                                        previousNode =
                                            previousNode.Children.LastOrDefault(c =>
                                                c.TagName == "UL");

                                        if (previousNode != null &&
                                            previousNode.TextContent.Contains(
                                                "The question was put that "))
                                        {
                                            lastQuestionDescription = previousNode.TextContent
                                                .Replace("The question was put that ", "That ")
                                                .Trim();
                                            // Remove the period from the end
                                            lastQuestionDescription =
                                                lastQuestionDescription.Substring(0,
                                                    lastQuestionDescription.Length - 1);
                                        }
                                    }
                                }
                                
                                if (lastQuestionDescription == null)
                                {
                                    int i = 0;
                                }

                                // Extract various information about the question from the description and subtitle
                                var extractionResults =
                                    GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle,
                                        lastQuestionDescription);
                                lastQuestionType = extractionResults.Item2;
                                lastStage = extractionResults.Item1 ?? lastStage;
                                lastQuestionTitle =
                                    extractionResults.Item3 ?? lastQuestionTitle;

                                // For an amendment, see if there is a member
                                Member amendMember = null;
                                var memberNameMatch = lastQuestionDescription
                                    .Replace(" the the ", " ").Replace(" the ", "")
                                    .Replace(" the Rt Hon ", " ").Replace(" the Hon ", " ")
                                    .Replace(" Rt Hon ", " ")
                                    .Replace(" Sir ", " ")
                                    .Replace(" Dame ", " ")
                                    .Replace(" Hon ", " ").Replace(" Dr ", " ")
                                    .Replace(" Vui ", " ")
                                    .Split(" in the name of ");

                                if (memberNameMatch.Length > 1)
                                {
                                    var nameStart = memberNameMatch[1].Trim().ToLower().Replace("’", "'");
                                    amendMember = _db.Members.FirstOrDefault(m =>
                                        nameStart.StartsWith((m.FirstName + " " + m.LastName)
                                            .ToLower()) ||
                                        nameStart.StartsWith(m.AlsoKnownAs.ToLower()));

                                    if (amendMember == null)
                                        throw new Exception(
                                            "Mover not found in DB, member name: " + nameStart);
                                }

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
                                question.Member = amendMember ?? lastMember;
                                question.Parliament = currentParliament;

                                // Find if there is a bill associated with this question
                                if (question.Title.Contains(" Bill"))
                                {
                                    var bill = _db.Bills.FirstOrDefault(b =>
                                        b.Title.ToLower() == question.Title.ToLower());

                                    if (bill != null)
                                    {
                                        question.Title = bill.Title;
                                        question.Bill = bill;
                                    }
                                }

                                // Find if there is a SOP associated with this question
                                Match sopMatch = Regex.Match(question.Description,
                                    @"Supplementary Order Paper ([0-9]+)");
                                if (sopMatch.Success &&
                                    !question.Description.Contains(" amendment "))
                                {
                                    var sopNumber = int.Parse(sopMatch.Groups[1].Value);

                                    var sop = _db.SupplementaryOrderPapers.FirstOrDefault(s =>
                                        s.Number == sopNumber);

                                    if (sop != null)
                                    {
                                        question.SupplementaryOrderPaper = sop;
                                        question.Member = sop.Member;
                                    }
                                }

                                // Remove any existing personal votes for this question
                                var existingPersonalVotesVotes =
                                    _db.PersonalVotes.Where(p => p.Question_Id == question.Id);
                                _db.PersonalVotes.RemoveRange(existingPersonalVotesVotes);

                                // Clear the description and clause
                                lastQuestionDescription = null;
                                if (amendMember == null)
                                    lastMember = null;

                                // Iterate through the child nodes for a personal vote
                                foreach (var personalVoteComponent in speech.Children)
                                {
                                    // If the party vote section also contains a voice vote
                                    if (personalVoteComponent.TagName == "UL" ||
                                        personalVoteComponent.ClassList.Contains("section"))
                                    {
                                        string voiceVoteContent = personalVoteComponent.TextContent.Trim();

                                        if ((voiceVoteContent.EndsWith("agreed to.") ||
                                             (voiceVoteContent.StartsWith("Bill read ") &&
                                              voiceVoteContent.EndsWith(" time."))) &&
                                            !voiceVoteContent.Contains("The question was put that "))
                                        {
                                            var result = ProcessVoiceVote(personalVoteComponent,
                                                voiceVoteContent,
                                                lastQuestionTitle, lastQuestionSubtitle,
                                                lastQuestionCommitteeTitle,
                                                lastQuestionDescription, lastStage, lastDate, currentParliament,
                                                lastMember);

                                            newQuestions.Add(result.Item1);

                                            // Remove the last question description and clause, as there should only be one question for each of these
                                            lastQuestionDescription = null;
                                            if (result.Item2 == null)
                                                lastMember = null;
                                            lastQuestionType = QuestionType.Motion;
                                        }
                                    }

                                    else if (personalVoteComponent.ClassList.Contains("vote"))
                                    {
                                        bool? lastPosition = null;
                                        string lastComplexPosition = null;

                                        var captionNode =
                                            personalVoteComponent.QuerySelector("caption");

                                        if (captionNode != null)
                                        {
                                            // Parse out the actual position, ignoring vote totals (as we calculate these ourselves)
                                            string positionText = captionNode.TextContent.Trim();
                                            Match positionMatch = Regex.Match(positionText,
                                                @"(.+)[ \n]+([0-9]+)");
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

                                        var memberNodes = personalVoteComponent.QuerySelectorAll("tbody td");

                                        foreach (var memberNode in memberNodes)
                                        {
                                            // Get the member name, determine if they voted by proxy and then remove the ' (P)' and 'Teller:' if necessary
                                            var memberName = memberNode.TextContent.Replace("Teller:", "")
                                                .Replace("Teller", "").Trim();
                                            if (!string.IsNullOrEmpty(memberName))
                                            {
                                                var isProxy = memberName.Contains("(P)");
                                                memberName = memberName.Replace("(P)", "").Replace("’", "'").Trim();
                                                
                                                // Quick fix for H V Ross Robertson
                                                memberName = memberName.Replace("Robertson H", "Robertson R");

                                                // Find the member in the DB, by their last name, or alternately by their last name and their first initial
                                                var member = _db.Members.FirstOrDefault(m =>
                                                    (m.LastName == memberName ||
                                                     m.LastName + " " + m.FirstName.Substring(0, 1) ==
                                                     memberName ||
                                                     m.FirstName.Substring(0, 1) + " " + m.LastName == memberName ||
                                                     m.FirstName + " " + m.LastName == memberName ||
                                                     m.AlsoKnownAs.Contains(memberName)) && m.Tenures.Any(t =>
                                                        t.Start <= sittingDate &&
                                                        (t.End == null || t.End >= sittingDate)));

                                                if (member == null)
                                                    throw new Exception("Member not found in DB. Member name " +
                                                                        memberName);

                                                // Add the new personal vote to the DB
                                                PersonalVote personalVote = new PersonalVote();
                                                await _db.PersonalVotes.AddAsync(personalVote);

                                                personalVote.Proxy = isProxy;
                                                personalVote.Member_Id = member.Id;

                                                // Update the personal vote
                                                personalVote.Update(question, lastPosition,
                                                    lastComplexPosition);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                lastQuestionTitle = null;
                lastQuestionSubtitle = null;
                lastQuestionDescription = null;
                lastMember = null;
                lastQuestionCommitteeTitle = null;
                lastStage = null;
                lastQuestionType = QuestionType.Motion;
            }

            await _db.Questions.AddRangeAsync(newQuestions);

            await _db.SaveChangesAsync();
        }


        private (Question, Member) ProcessVoiceVote(IElement speechLine, string voiceVoteContent,
            string lastQuestionTitle, string lastQuestionSubtitle, string lastQuestionCommitteeTitle,
            string lastQuestionDescription, Stage? lastStage, DateTime lastDate, Parliament currentParliament,
            Member lastMember)
        {
            // The previous node might have a better description
            var previousNode = speechLine.PreviousElementSibling;

            // Ignore any clauses in previous siblings
            while (previousNode != null &&
                   previousNode.ClassList.Any(c => c.ToLower().Contains("clause")))
            {
                previousNode = previousNode.PreviousElementSibling;
            }

            if (previousNode != null &&
                previousNode.TextContent.Contains("The question was put that "))
            {
                lastQuestionDescription = previousNode.TextContent
                    .Replace("The question was put that ", "That ").Trim();
                // Remove the period from the end
                lastQuestionDescription =
                    lastQuestionDescription.Substring(0,
                        lastQuestionDescription.Length - 1);
            }

            if (lastQuestionDescription == null)
            {
                // As a last resort, use the result of the vote as a description, but remove the agreement information
                lastQuestionDescription = voiceVoteContent
                    .Replace(" not agreed to.", "")
                    .Replace(" agreed to.", "").Trim();
            }

            if (lastQuestionDescription == null)
            {
                int i = 0;
            }
            
            // Extract various information about the question from the description and subtitle
            var extractionResults =
                GetStageQuestionTypeAndFixedTitle(lastQuestionSubtitle,
                    lastQuestionDescription);
            QuestionType lastQuestionType = extractionResults.Item2;
            lastStage = extractionResults.Item1 ?? lastStage;
            lastQuestionTitle = extractionResults.Item3 ?? lastQuestionTitle;

            // Create the question
            Question question = new Question();

            // If the motion is simply about votes, then provide more detail
            if (lastQuestionDescription == "Votes" &&
                lastQuestionCommitteeTitle != null)
            {
                lastQuestionDescription = lastQuestionCommitteeTitle;
            }

            // For an amendment, see if there is a member
            Member amendMember = null;
            var memberNameMatch = lastQuestionDescription.Replace(" the the ", " ")
                .Replace(" the ", "")
                .Replace(" the Rt Hon ", " ").Replace(" the Hon ", " ")
                .Replace(" Rt Hon ", " ")
                .Replace(" Sir ", " ")
                .Replace(" Dame ", " ")
                .Replace(" Hon ", " ").Replace(" Dr ", " ").Replace(" Vui ", " ").Split(" in the name of ");

            if (memberNameMatch.Length > 1)
            {
                var nameStart = memberNameMatch[1].Trim().ToLower().Replace("’", "'");
                amendMember = _db.Members.FirstOrDefault(m =>
                    nameStart.StartsWith((m.FirstName + " " + m.LastName).ToLower()) ||
                    nameStart.StartsWith(m.AlsoKnownAs.ToLower()));

                if (amendMember == null)
                    throw new Exception("Mover not found in DB, member name: " +
                                        nameStart);
            }

            // Update the question as necessary
            question.Title = lastQuestionTitle;
            question.Subtitle = lastQuestionSubtitle;
            question.Description = lastQuestionDescription;
            question.Timestamp = lastDate;
            question.Stage = lastStage;
            question.QuestionType = lastQuestionType;
            question.Member = amendMember ?? lastMember;
            question.Parliament = currentParliament;

            // Find if there is a bill associated with this question
            if (question.Title.Contains(" Bill"))
            {
                var bill = _db.Bills.FirstOrDefault(b =>
                    b.Title.ToLower() == question.Title.ToLower());

                if (bill != null)
                {
                    question.Title = bill.Title;
                    question.Bill = bill;
                }
            }

            // Find if there is a SOP associated with this question
            Match sopMatch = Regex.Match(question.Description,
                @"Supplementary Order Paper ([0-9]+)");
            if (sopMatch.Success && !question.Description.Contains(" amendment "))
            {
                var sopNumber = int.Parse(sopMatch.Groups[1].Value);

                var sop = _db.SupplementaryOrderPapers.FirstOrDefault(s =>
                    s.Number == sopNumber);

                if (sop != null)
                {
                    question.SupplementaryOrderPaper = sop;
                    question.Member = sop.Member;
                }
            }

            // See if this voice vote already exists
            VoiceVote vote =
                _db.VoiceVotes.FirstOrDefault(v => v.Question_Id == question.Id);

            // If the vote doesn't exist, then we need to create a new one
            if (vote == null)
            {
                vote = new VoiceVote();
                _db.VoiceVotes.Add(vote);
            }

            // Update the vote
            vote.Question = question;
            vote.Position = !voiceVoteContent.Contains(" not agreed to");
            return (question, amendMember);
        }

        private (Stage?, QuestionType, string) GetStageQuestionTypeAndFixedTitle(string questionSubtitle,
            string questionDescription)
        {
            string questionTitle = null;
            QuestionType type = QuestionType.Motion;
            Stage? stage = null;

            // The bill is being read
            if (questionDescription.Contains(" read ") ||
                (questionSubtitle != null && questionSubtitle.Contains(" Reading")))
            {
                // Extract the bill title with correct capitalisation
                int endIndex = questionDescription.IndexOf(" be now read");
                if (endIndex != -1)
                {
                    string newQuestionTitle = questionDescription.Substring(0, endIndex).Replace("That the ", "")
                        .Replace("That the", "").Replace("That ", "").Trim();
                    if (newQuestionTitle != "bill")
                        questionTitle = newQuestionTitle;
                }

                type = QuestionType.BillReading;

                if (questionDescription.Contains("first time") ||
                    (questionSubtitle != null && questionSubtitle.Contains("First ")))
                    stage = Stage.FirstReading;
                else if (questionDescription.Contains("second time") ||
                         (questionSubtitle != null && questionSubtitle.Contains("Second ")))
                    stage = Stage.SecondReading;
                else if (questionDescription.Contains("third time") ||
                         (questionSubtitle != null && questionSubtitle.Contains("Third ")))
                    stage = Stage.ThirdReading;
            }

            // The bill is in committee
            if (questionSubtitle != null && (questionSubtitle.Contains("Committee") ||
                                             questionSubtitle.ToLower().Contains("part") ||
                                             questionSubtitle.ToLower().Contains("schedule") ||
                                             questionSubtitle.ToLower().Contains("clause")))
            {
                stage = Stage.Committee;
                type = QuestionType.BillPart;

                if (questionDescription.Contains(" the following amendment "))
                    type = QuestionType.Amendment;
                else if (questionDescription.Contains("Supplementary Order Paper"))
                    type = QuestionType.SupplementaryOrderPaper;
                else if (questionDescription.Contains("amendment"))
                    type = QuestionType.Amendment;
                else if (questionDescription.Contains("question"))
                    type = QuestionType.Motion;
            }

            return (stage, type, questionTitle);
        }
    }
}