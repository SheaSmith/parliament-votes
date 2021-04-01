using Microsoft.AspNetCore.Mvc;
using ParliamentVotes.Data;
using ParliamentVotes.ViewModels.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ApplicationDbContext db;

        public StatisticsController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        [Route("topline")]
        public IActionResult GetTopline()
        {
            var get = new ToplineGet();

            get.PartyVotes = db.PartyVotes.Count();
            get.VoiceVotes = db.VoiceVotes.Count();
            get.PersonalVotes = db.PersonalVotes.Count();
            get.Members = db.Members.Count();
            get.Votes = get.PartyVotes + get.VoiceVotes + get.PersonalVotes;

            get.Parliaments = db.Parliaments.Count();

            var min = db.Parliaments.FirstOrDefault().StartDate;

            // Get the actual parties we cover, rather than just everything in our db
            get.Parties = db.Tenures.Where(t => t.Start >= min && t.Party_Id != null).Select(t => t.Party).Distinct().Count();

            // Strictly speaking, this isn't exactly correct, but it's close enough for the query to be performant
            get.Years = (int)((DateTime.Today - db.Parliaments.FirstOrDefault().StartDate).TotalDays / 365.2425);

            return Ok(get);
        }
    }
}
