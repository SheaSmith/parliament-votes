using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParliamentVotes.Data;
using ParliamentVotes.Models.Motions;
using ParliamentVotes.Models.Organisational;
using ParliamentVotes.Models.Votes;
using ParliamentVotes.ViewModels;

namespace ParliamentVotes.Controllers
{

    [Route("private-api/import")]
    [ApiController]
    public class DataImportController : ControllerBase
    {
        private static string[] emptyMotions = new string[] { "That the amendment be agreed to.", "That the amendments be agreed to.", "That the amendments to the amendments be agreed to.", "That the amendment to the amendments be agreed to.", "That the amendment to the amendment be agree to.", "That the amendments to the amendment be agreed to.", "That the motion be agreed to." };

        private ApplicationDbContext db;

        public DataImportController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet("mps")]
        public async Task<IActionResult> ImportMPsAndParties()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var allCurrentMpsDocument = await context.OpenAsync("https://www.parliament.nz/en/MpBioListingAjax/CurrentListing/13825");

            var currentMps = allCurrentMpsDocument.QuerySelectorAll(".list__cell-body a.list__cell-heading");

            var formerMpsDocument = await context.OpenAsync("https://www.parliament.nz/en/MpBioListingAjax/CurrentListing/13826");

            var formerMps = formerMpsDocument.QuerySelectorAll(".list__cell-body a.list__cell-heading").ToList();

            var mps = currentMps.ToList();
            mps.AddRange(formerMps);

            List<Party> parties = db.Parties.ToList();

            List<Member> members = db.Members.ToList();
            List<Tenure> tenures = db.Tenures.ToList();

            List<Member> membersToAdd = new List<Member>();
            List<Tenure> tenuresToAdd = new List<Tenure>();

            var tasks = mps.Select(async mp =>
            {
                await ProcessMpsProfile(mp.GetAttribute("href"), context, members, tenures, tenuresToAdd, membersToAdd, parties);
            });

            await Task.WhenAll(tasks);

            db.UpdateRange(members);
            db.UpdateRange(tenures);

            db.AddRange(membersToAdd);
            db.AddRange(tenuresToAdd);

            await db.SaveChangesAsync();

            return Ok(db.Members.Select(m => new MemberGetModel(m)));
        }

        private async Task ProcessMpsProfile(string href, IBrowsingContext context, List<Member> members, List<Tenure> tenures, List<Tenure> tenuresToAdd, List<Member> membersToAdd, List<Party> parties)
        {
            var mpDocument = await context.OpenAsync("https://www.parliament.nz" + href);

            var tables = mpDocument.QuerySelectorAll("table");

            if (tables.Length == 0)
            {
                return;
            }

            var name = mpDocument.QuerySelector("title").TextContent.Split(" - ")[0].Trim().Split(", ");

            var firstName = name[1].Trim();
            var lastName = name[0].Trim();

            Member member = members.Where(d => d.FirstName == firstName && d.LastName == lastName).FirstOrDefault();

            if (member == null)
            {
                member = new Member(firstName, lastName);
                membersToAdd.Add(member);
            }

            var images = mpDocument.QuerySelectorAll(".document-panel__img img");
            member.ImageUrl = "https://www.parliament.nz" + images[images.Length - 2].GetAttribute("data-original");

            foreach (var table in tables)
            {
                var firstRow = table.QuerySelector("thead td");

                if (firstRow == null)
                {
                    firstRow = table.QuerySelector("thead th");
                }

                if (firstRow == null)
                {
                    continue;
                }


                if (firstRow.TextContent.Trim().Replace(" of ", " for ") == "Member for / List")
                {
                    foreach (var row in table.QuerySelectorAll("tbody tr"))
                    {
                        var cells = row.QuerySelectorAll("td");

                        string partyName = cells[1].TextContent.Trim();

                        // Custom fix just for ACT
                        partyName = partyName.Replace("ACT New Zealand", "ACT");

                        // Custom fix for labour
                        if (partyName == "Labour")
                            partyName = "Labour Party";

                        // Custom fix for the Maori party
                        if (partyName == "Maori Party")
                            partyName = "Māori Party";

                        Party party;

                        lock (parties)
                        {
                            party = parties.Where(p => p.Name == partyName).FirstOrDefault();

                            if (party == null)
                            {
                                party = new Party(partyName);
                                parties.Add(party);
                            }
                        }

                        DateTime? end = null;
                        if (cells.Length == 4 && cells[3].TextContent.Trim() != "")
                        {
                            end = DateTime.Parse(cells[3].TextContent.Trim(), new CultureInfo("en-NZ"));
                        }

                        string electorate = cells[0].TextContent.Trim();

                        if (electorate == "List")
                        {
                            electorate = null;
                        }

                        var beginString = cells[2].TextContent.Trim();

                        DateTime begin;

                        if (beginString.Contains("-"))
                        {
                            var dates = beginString.Split("-");
                            begin = DateTime.ParseExact(dates[0].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                            end = DateTime.ParseExact(dates[1].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                        }
                        else
                        {
                            begin = DateTime.Parse(beginString, new CultureInfo("en-NZ"));
                        }

                        Tenure tenure = tenures.Where(t => t.Member_Id == member.Id && t.Start == begin).FirstOrDefault();

                        if (tenure == null)
                        {
                            tenure = new Tenure();
                            tenuresToAdd.Add(tenure);
                        }

                        tenure.Update(member, begin, party, electorate, end);

                    }

                    break;
                }
            }
        }

        [HttpGet("question/by-hansard-day")]
        public async Task<IActionResult> QuestionByHansardDay(string url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var questionDocument = await context.OpenAsync(url);

            var debates = questionDocument.QuerySelectorAll(".hansard__level li");

            var sittingDate = DateTime.ParseExact(questionDocument.QuerySelector(".publish-date").TextContent.Replace("Sitting date:", "").Trim(), "d MMM yyyy", new CultureInfo("en-NZ"));

            string questionTitle = null;
            string questionSubtitle = null;
            string questionDescription = null;
            string questionSubSubTitle = null;

            List<Question> newQuestions = new List<Question>();

            foreach (var debate in debates)
            {
                var debateTitle = debate.QuerySelector(".Debate");

                if (debateTitle == null)
                    debateTitle = debate.QuerySelector(".BillDebate");

                if (debateTitle == null)
                    debateTitle = debate.QuerySelector(".Debatealone");

                if (debateTitle != null)
                {
                    var newQuestionTitle = debateTitle.TextContent.Trim().Replace("  ", " ");
                    // Set question title to title case
                    newQuestionTitle = string.Join(" ", newQuestionTitle.Split(' ').Select(i => i.Substring(0, 1).ToUpper() + i.Substring(1).ToLower()).ToArray());

                    if (questionTitle != newQuestionTitle && questionSubtitle != null)
                    {
                        questionSubtitle = null;
                        questionSubSubTitle = null;
                    }

                    questionTitle = newQuestionTitle;
                }

                var subdebate = debate.QuerySelector(".SubDebate");

                if (subdebate == null)
                    subdebate = debate.QuerySelector(".SubDebatealone");

                if (subdebate != null)
                {
                    questionSubtitle = subdebate.TextContent.Trim();

                    if (questionSubtitle.Contains("Reading"))
                        questionSubtitle = null;
                }

                var marginHeading = debate.QuerySelector(".MarginHeading");

                if (marginHeading != null)
                    questionSubSubTitle = marginHeading.TextContent;

                if (debate.TextContent.Contains("I move") || debate.TextContent.Contains("The question now is") || debate.TextContent.Contains("The question was put that"))
                {
                    var speechNode = debate.QuerySelector(".Speech");

                    IHtmlCollection<IElement> speech = null;

                    if (speechNode != null)
                    {
                        speech = speechNode.QuerySelectorAll("em");
                    }

                    string motion = null;

                    if (speech != null && speech.Length == 0)
                    {
                        speechNode = debate.QuerySelector(".ContinueSpeech");
                        if (speechNode != null)
                            speech = speechNode.QuerySelectorAll("em");
                    }

                    if (speech != null && speech.Length != 0)
                        motion = string.Join("", speech.Select(m => m.TextContent)).Trim();

                    if (motion == null)
                    {
                        var committeeQuestionPut = debate.QuerySelectorAll(".IndentMarginTextFollowing");
                        
                        foreach (var committeeQuestion in committeeQuestionPut)
                        {
                            if (committeeQuestion.TextContent.Contains("The question was put that "))
                            {
                                motion = committeeQuestion.TextContent.Replace("The question was put that ", "That ").Trim();
                            }
                        }
                    }

                    questionDescription = motion;
                }


                // Find all voice votes
                var voiceVotes = debate.QuerySelectorAll(".IndentMarginalone");

                if (voiceVotes != null && questionTitle != null)
                {
                    foreach (var voiceVote in voiceVotes)
                    {
                        var text = voiceVote.TextContent;

                        if ((text.Contains("Bill read a ") && text.Contains(" time.")) || text.Contains("agreed to"))
                        {
                            var question = db.Questions.FirstOrDefault(q => q.Timestamp == sittingDate && q.QuestionTitle == questionTitle && q.QuestionDescription == questionDescription && q.QuestionSubtitle == questionSubtitle);

                            if (question == null)
                            {
                                question = newQuestions.FirstOrDefault(q => q.Timestamp == sittingDate && q.QuestionTitle == questionTitle && q.QuestionDescription == questionDescription && q.QuestionSubtitle == questionSubtitle);
                            }

                            if (question == null)
                            {
                                question = new Question();
                                newQuestions.Add(question);
                            }

                            if (questionDescription != null && questionDescription.Contains(" read "))
                            {
                                int endIndex = questionDescription.IndexOf(" be now read");
                                questionTitle = questionDescription.Substring(0, endIndex).Replace("That the ", "").Trim();
                                question.QuestionType = QuestionType.BillReading;

                                if (questionDescription.Contains("first time"))
                                    question.Stage = Stage.FirstReading;
                                else if (questionDescription.Contains("second time"))
                                    question.Stage = Stage.SecondReading;
                                else if (questionDescription.Contains("third time"))
                                    question.Stage = Stage.ThirdReading;
                            }
                            else
                            {
                                question.QuestionType = QuestionType.Motion;
                            }

                            if (questionSubtitle != null && questionSubtitle.ToLower().Contains("committee"))
                            {
                                question.Stage = Stage.Committee;
                                question.QuestionType = QuestionType.BillPart;

                                questionSubtitle = questionSubSubTitle;

                                if (questionDescription != null && questionDescription.Contains("Supplementary Order Paper"))
                                    question.QuestionType = QuestionType.SupplementaryOrderPaper;
                                else if (questionDescription != null && questionDescription.Contains("amendment"))
                                    question.QuestionType = QuestionType.Amendment;
                            }

                            if (text.ToLower().Contains("part") || text.ToLower().Contains("section") || text.ToLower().Contains("clause") || text.ToLower().Contains("schedule"))
                                questionDescription = text.Replace(" not agreed to.", "").Replace(" agreed to.", "");

                            if (questionDescription != null && emptyMotions.Contains(questionDescription))
                            {
                                var beforeNode = voiceVote.PreviousElementSibling;

                                if (beforeNode != null && beforeNode.ClassList.Contains("clause"))
                                    beforeNode = beforeNode.PreviousElementSibling;

                                if (beforeNode != null && beforeNode.TextContent.Contains("The question was put that "))
                                {
                                    questionDescription = beforeNode.TextContent.Replace("The question was put that ", "That ");
                                    questionDescription = questionDescription.Substring(0, questionDescription.Length - 1);
                                }
                            }

                            question.QuestionTitle = questionTitle;
                            question.QuestionSubtitle = questionSubtitle;
                            question.QuestionDescription = questionDescription;
                            question.Timestamp = sittingDate;

                            VoiceVote vote = db.VoiceVotes.FirstOrDefault(v => v.Question_Id == question.Id);

                            if (vote == null)
                            {
                                vote = new VoiceVote();
                                db.VoiceVotes.Add(vote);
                            }

                            vote.Question = question;
                            vote.Position = !text.Contains("Motion not agreed to");

                            questionDescription = null;
                        }
                    }
                }

                var partyVotes = debate.QuerySelectorAll(".VoteReason");

                bool table = false;

                if (partyVotes.Length == 0)
                {
                    partyVotes = questionDocument.QuerySelectorAll(".vote caption");
                    table = true;
                }

                if (partyVotes != null)
                {
                    for (int i = 0; i != partyVotes.Length; i++)
                    {
                        string motion = null;
                        var questionDescriptionNodes = partyVotes[i].QuerySelectorAll("em");

                        if (questionDescriptionNodes.Length != 0)
                        {
                            motion = string.Join("", questionDescriptionNodes.Select(m => m.TextContent)).Trim();
                        }

                        if ((motion.Contains("That the amendment be agreed to") || motion.Contains("That the amendments be agreed to") || motion.Contains("That the motion be agreed to")) && questionDescription != null)
                        {
                            motion = questionDescription;
                        }



                        // We have a question
                        var question = db.Questions.FirstOrDefault(q => q.QuestionTitle == questionTitle && q.QuestionSubtitle == questionSubtitle && q.QuestionDescription == motion && q.Timestamp == sittingDate);

                        if (question == null)
                        {
                            question = newQuestions.FirstOrDefault(q => q.QuestionTitle == questionTitle && q.QuestionSubtitle == questionSubtitle && q.QuestionDescription == motion && q.Timestamp == sittingDate);
                        }

                        if (question == null)
                        {
                            question = new Question();
                            newQuestions.Add(question);
                        }

                        question.QuestionTitle = questionTitle;
                        question.Timestamp = sittingDate;
                        question.QuestionSubtitle = questionSubtitle;

                        if (motion != null)
                        {
                            question.QuestionDescription = motion;

                            if (motion.Contains(" read "))
                            {
                                int endIndex = motion.IndexOf(" be now read");

                                if (endIndex == -1)
                                    endIndex = motion.IndexOf(" be read ");

                                questionTitle = motion.Substring(0, endIndex).Replace("That the ", "").Trim();
                                question.QuestionTitle = questionTitle;
                                question.QuestionType = QuestionType.BillReading;

                                if (motion.Contains("first time"))
                                    question.Stage = Stage.FirstReading;
                                else if (motion.Contains("second time"))
                                    question.Stage = Stage.SecondReading;
                                else if (motion.Contains("third time"))
                                    question.Stage = Stage.ThirdReading;
                            }
                            else
                            {
                                question.QuestionType = QuestionType.Motion;
                            }
                        }

                        if (question.QuestionDescription != null && emptyMotions.Contains(question.QuestionDescription))
                        {
                            var beforeNode = partyVotes[i].PreviousElementSibling;

                            if (beforeNode != null && beforeNode.ClassList.Contains("clause"))
                                beforeNode = beforeNode.PreviousElementSibling;

                            if (beforeNode != null && beforeNode.TextContent.Contains("The question was put that "))
                            {
                                question.QuestionDescription = beforeNode.TextContent.Replace("The question was put that ", "That ");
                                question.QuestionDescription = question.QuestionDescription.Substring(0, question.QuestionDescription.Length - 1);
                            }
                        }

                        if (question.QuestionSubtitle != null && question.QuestionSubtitle.ToLower().Contains("committee"))
                        {
                            question.Stage = Stage.Committee;
                            question.QuestionType = QuestionType.BillPart;

                            questionSubtitle = null;
                            question.QuestionSubtitle = questionSubSubTitle;

                            if (question.QuestionDescription != null && question.QuestionDescription.Contains("Supplementary Order Paper"))
                                question.QuestionType = QuestionType.SupplementaryOrderPaper;
                            else if (question.QuestionDescription != null && question.QuestionDescription.Contains("amendment"))
                                question.QuestionType = QuestionType.Amendment;
                        }

                        if (partyVotes[i].TextContent.Contains("party vote"))
                        {
                            // Properties for legacy tables
                            int tableIndex = 0;
                            List<IElement> tableRows = null;

                            // We have a party vote, so treat as such
                            IElement voteElement;
                            if (!table)
                            {
                                voteElement = partyVotes[i].NextElementSibling;
                            }
                            else
                            {
                                tableRows = partyVotes[i].ParentElement.QuerySelectorAll("tbody tr").SelectMany(t => t.Children).ToList();
                                voteElement = tableRows[tableIndex];
                            }

                            bool? lastPosition = null;
                            string lastComplexPosition = null;

                            while (voteElement != null && !voteElement.ClassList.Contains("VoteResult"))
                            {
                                // Get the position of the parties
                                if (voteElement.ClassList.Contains("VoteCount"))
                                {
                                    string positionText = voteElement.TextContent.Trim();
                                    Match positionMatch = Regex.Match(positionText, @"(.+)[ \n]+([0-9]+)");
                                    positionText = positionMatch.Groups[1].Value.Trim();

                                    if (positionText == "Ayes")
                                    {
                                        lastPosition = true;
                                        lastComplexPosition = null;
                                    }
                                    else if (positionText == "Noes")
                                    {
                                        lastPosition = false;
                                        lastComplexPosition = null;
                                    }
                                    else if (positionText == "Abstentions")
                                    {
                                        lastComplexPosition = "Abstain";
                                        lastPosition = null;
                                    }
                                    else
                                    {
                                        lastComplexPosition = positionText;
                                        lastPosition = null;
                                    }
                                }

                                else if (voteElement.ClassList.Contains("VoteText"))
                                {
                                    // Remove period from end and split by party
                                    string votedText = voteElement.TextContent.Trim();
                                    var voters = votedText.Substring(0, votedText.Length - 1).Split(";");

                                    foreach (string voter in voters)
                                    {
                                        var partyMatch = Regex.Match(voter, @"(.+) ([0-9]+)");

                                        if (partyMatch.Success)
                                        {
                                            string partyName = partyMatch.Groups[1].Value.Trim();
                                            int partyNumbers = int.Parse(partyMatch.Groups[2].Value);

                                            var party = db.Parties.FirstOrDefault(p => p.Name == partyName || p.AlsoKnownAs == partyName);

                                            if (party == null)
                                                return BadRequest("Party not found in DB. Party name " + partyName);

                                            var existingPartyVotes = db.PartyVotes.Where(p => p.Party_Id == party.Id && p.Question_Id == question.Id);

                                            PartyVote partyVote;
                                            if (existingPartyVotes.Count() > 1)
                                            {
                                                partyVote = existingPartyVotes.FirstOrDefault(p => p.Position == lastPosition && p.ComplexPosition == lastComplexPosition);
                                            }
                                            else
                                            {
                                                partyVote = existingPartyVotes.FirstOrDefault();
                                            }

                                            if (partyVote == null)
                                            {
                                                partyVote = new PartyVote();
                                                db.PartyVotes.Add(partyVote);
                                            }

                                            Match splitPartyVoteMatch = Regex.Match(voter, @"(.+) ([0-9]+) \((.+)\)");

                                            List<SplitPartyVote> splitPartyVotes = null;
                                            if (splitPartyVoteMatch.Success)
                                            {
                                                // This is a split party vote, and needs to be treated accordingly
                                                var members = splitPartyVoteMatch.Groups[3].Value.Split(", ");
                                                List<Member> splitMembers = db.Members.Where(m => (members.Contains(m.LastName) || members.Contains(m.LastName + " " + m.FirstName.Substring(0, 1))) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate))).ToList();

                                                splitPartyVotes.AddRange(splitMembers.Select(m => new SplitPartyVote(partyVote, m)));
                                            }

                                            partyVote.Update(question, partyNumbers, lastPosition, party, complexPosition: lastComplexPosition);
                                        }
                                        else
                                        {
                                            var memberName = voter.Replace("Independent: ", "").Trim();
                                            var member = db.Members.FirstOrDefault(m => (m.LastName == memberName || m.LastName + " " + m.FirstName.Substring(0, 1) == memberName) && m.Tenures.Any(t => t.Start <= sittingDate && (t.End == null || t.End >= sittingDate)));

                                            if (member == null)
                                                return BadRequest("Member not found in DB. Member name " + memberName);

                                            PartyVote partyVote = db.PartyVotes.FirstOrDefault(p => p.Member_Id == member.Id && p.Question_Id == question.Id);

                                            if (partyVote == null)
                                            {
                                                partyVote = new PartyVote();
                                                db.PartyVotes.Add(partyVote);
                                            }

                                            partyVote.Update(question, 1, member: member, position: lastPosition, complexPosition: lastComplexPosition);
                                        }
                                    }
                                }

                                if (!table)
                                {
                                    voteElement = voteElement.NextElementSibling;
                                }
                                else
                                {
                                    voteElement = tableRows[++tableIndex];
                                }
                            }

                            questionDescription = null;
                        }
                    }
                }
            }

            // Filter out any questions that weren't voted on
            newQuestions = newQuestions.Where(q => db.PartyVotes.Any(p => p.Question == q) || db.PersonalVotes.Any(p => p.Question == q) || db.VoiceVotes.Any(v => v.Question == q)).ToList();

            db.Questions.AddRange(newQuestions);

            await db.SaveChangesAsync();

            return Ok();
        }


        //[HttpGet("question/by-url")]
        //public async Task<IActionResult> QuestionByUrl(string url)
        //{
        //    var config = Configuration.Default.WithDefaultLoader();
        //    var context = BrowsingContext.New(config);
        //    var questionDocument = await context.OpenAsync(url);

        //    var questionTitle = questionDocument.QuerySelector(".main > h1").TextContent.Replace("(continued)", "").Trim();

        //    Question question = db.Questions.FirstOrDefault(q => q.QuestionTitle == questionTitle);

        //    if (question == null)
        //    {
        //        question = new Question();
        //        db.Questions.Add(question);
        //    }

        //    var sittingDate = DateTime.ParseExact(questionDocument.QuerySelector(".publish-date").TextContent.Replace("Sitting date:", "").Trim(), "d MMM yyyy", new CultureInfo("en-NZ"));
        //    question.Timestamp = sittingDate;

        //    if (questionTitle.Contains("Reading"))
        //    {
        //        // Reading of a specific bill
        //        question.QuestionTitle = questionTitle;

        //        question.BillTitle = questionTitle.Split(" — ")[0];

        //        var voteReason = questionDocument.QuerySelector(".VoteReason");

        //        if (voteReason == null)
        //        {
        //            voteReason = questionDocument.QuerySelector(".vote caption");
        //        }

        //        if (voteReason == null)
        //        {
        //            // May be voice vote, or no vote occurred at all
        //        }
        //        else
        //        {
        //            question.QuestionType = QuestionType.BillReading;

        //            if (questionTitle.Contains("First Reading"))
        //            {
        //                question.Stage = Stage.FirstReading;
        //            }
        //            else if (questionTitle.Contains("Second Reading"))
        //            {
        //                question.Stage = Stage.SecondReading;
        //            }
        //            else if (questionTitle.Contains("Third Reading"))
        //            {
        //                question.Stage = Stage.ThirdReading;
        //            }

        //            if (voteReason.TextContent.Contains("party vote"))
        //            {
        //                // Party vote was called for
        //                question.QuestionDescription = voteReason.TextContent.Replace("A party vote was called for on the question,", "").Trim();

        //                var votes = questionDocument.QuerySelectorAll(".VoteText");
        //                var votesPosititions = questionDocument.QuerySelectorAll(".VoteCount");

        //                for (int i = 0; i != votes.Length; i++)
        //                {
        //                    var vote = votes[i];

        //                    var positionText = votesPosititions[i].TextContent.Trim();
        //                    var positionMatch = Regex.Match(positionText, @"(.+)[ \n]+([0-9]+)");
        //                    positionText = positionMatch.Groups[1].Value.Trim();

        //                    bool? position = null;
        //                    string complexPosition = null;

        //                    if (positionText == "Ayes")
        //                    {
        //                        position = true;
        //                    } 
        //                    else if (positionText == "Noes")
        //                    {
        //                        position = false;
        //                    } 
        //                    else if (positionText == "Abstentions")
        //                    {
        //                        complexPosition = "Abstain";
        //                    }
        //                    else
        //                    {
        //                        complexPosition = positionText;
        //                    }

        //                    string partiesVotedForText = vote.TextContent;
        //                    // Remove period from end and split by party
        //                    var partiesVotedFor = partiesVotedForText.Substring(0, partiesVotedForText.Length - 1).Split(";");

        //                    foreach (string partyVoteText in partiesVotedFor)
        //                    {
        //                        var match = Regex.Match(partyVoteText, @"(.+) ([0-9]+)");

        //                        if (match.Success)
        //                        {
        //                            string voter = match.Groups[1].Value.Trim();
        //                            int voteNumbers = int.Parse(match.Groups[2].Value);

        //                            var party = db.Parties.FirstOrDefault(p => p.Name == voter || p.AlsoKnownAs == voter);

        //                            if (party == null)
        //                                return BadRequest("Party not found in DB. Party name " + voter);

        //                            PartyVote partyVote = db.PartyVotes.FirstOrDefault(p => p.Party_Id == party.Id && p.Question_Id == question.Id);

        //                            if (partyVote == null)
        //                            {
        //                                partyVote = new PartyVote();
        //                                db.PartyVotes.Add(partyVote);
        //                            }

        //                            partyVote.Update(question, voteNumbers, position, party, complexPosition: complexPosition);
        //                        }
        //                        else
        //                        {
        //                            var memberVote = partyVoteText.Replace("Independent: ", "").Trim();
        //                            var member = db.Members.FirstOrDefault(m => m.LastName == memberVote || m.LastName + " " + m.FirstName.Substring(0, 1) == memberVote);

        //                            if (member == null)
        //                                return BadRequest("Member not found in DB. Member name " + memberVote);

        //                            PartyVote partyVote = db.PartyVotes.FirstOrDefault(p => p.Member_Id == member.Id && p.Question_Id == question.Id);

        //                            if (partyVote == null)
        //                            {
        //                                partyVote = new PartyVote();
        //                                db.PartyVotes.Add(partyVote);
        //                            }

        //                            partyVote.Update(question, 1, member: member, position: position, complexPosition: complexPosition);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    await db.SaveChangesAsync();
        //    return Ok();
        //}
    }

}