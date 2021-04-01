using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Data;
using ParliamentVotes.Models.Votes;
using ParliamentVotes.ViewModels;
using ParliamentVotes.ViewModels.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public QuestionsController(ApplicationDbContext db)
        {
            this.db = db;
        }

        /// <summary>
        /// Get the most recent questions
        /// </summary>
        /// <param name="limit">How many questions should be returned (max 100), defaults to 20</param>
        /// <param name="skip">How many questions should be skipped (e.g. for pages)</param>
        /// <returns></returns>
        [HttpGet]
        [Route("recent")]
        public IActionResult GetRecentQuestions(int limit = 20, int skip = 0, VoteType? voteType = null)
        {
            if (limit > 100)
            {
                return BadRequest(new Error("Cannot request more than 100 questions. Please use the data dump functionality"));
            }

            var questions = db.Questions
                .OrderByDescending(q => q.Timestamp)
                .Where(q => voteType == null || 
                    (voteType == VoteType.PartyVote && q.PartyVotes != null && q.PartyVotes.Count() != 0) || 
                    (voteType == VoteType.PersonalVote && q.PersonalVotes != null && q.PersonalVotes.Count() != 0) || 
                    (voteType == VoteType.VoiceVote && q.VoiceVotes != null && q.VoiceVotes.Count() != 0))
                .Skip(skip)
                .Take(limit)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.SplitPartyVotes)
                .ThenInclude(s => s.Tenures)
                .ThenInclude(q => q.Party)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.Party)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.PersonalVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.VoiceVotes);

            var count = db.Questions.Where(q => voteType == null ||
                    (voteType == VoteType.PartyVote && q.PartyVotes != null && q.PartyVotes.Count() != 0) ||
                    (voteType == VoteType.PersonalVote && q.PersonalVotes != null && q.PersonalVotes.Count() != 0) ||
                    (voteType == VoteType.VoiceVote && q.VoiceVotes != null && q.VoiceVotes.Count() != 0)).Count();

            return Ok(new Meta(count, questions.Select(q => new QuestionGet(q, true))));
        }

        [HttpGet]
        [Route("{id:int}")]
        public IActionResult GetById(int id)
        {
            var question = db.Questions
                .Include(q => q.Member.Tenures)
                .ThenInclude(q => q.Party)
                .Include(q => q.Bill.Parliaments)
                .ThenInclude(p => p.GoverningParties)
                .ThenInclude(g => g.Party)
                .Include(q => q.Bill.Members)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(q => q.Party)

                .Include(q => q.Bill.Questions)
                .ThenInclude(q => q.PartyVotes)
                .ThenInclude(p => p.SplitPartyVotes)
                .ThenInclude(s => s.Tenures)
                .ThenInclude(q => q.Party)
                .Include(q => q.Bill.Questions)
                .ThenInclude(q => q.PartyVotes)
                .ThenInclude(p => p.Party)
                .Include(q => q.Bill.Questions)
                .ThenInclude(q => q.PartyVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.Bill.Questions)
                .ThenInclude(q => q.PersonalVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.Bill.Questions)
                .ThenInclude(q => q.VoiceVotes)

                .Include(q => q.SupplementaryOrderPaper.Member.Tenures)
                .Include(q => q.SupplementaryOrderPaper.Parliament.GoverningParties)
                .ThenInclude(g => g.Party)
                .Include(q => q.Parliament.GoverningParties)
                .ThenInclude(g => g.Party)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.SplitPartyVotes)
                .ThenInclude(s => s.Tenures)
                .ThenInclude(q => q.Party)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.Party)
                .Include(q => q.PartyVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.PersonalVotes)
                .ThenInclude(p => p.Member)
                .ThenInclude(m => m.Tenures)
                .ThenInclude(t => t.Party)
                .Include(q => q.VoiceVotes)
                .FirstOrDefault(q => q.Id == id);

            if (question == null)
                return NotFound();

            return Ok(new QuestionGet(question));
        }
    }
}
