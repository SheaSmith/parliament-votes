using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParliamentVotes.Data;
using ParliamentVotes.ViewModels;

namespace ParliamentVotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartiesController : ControllerBase
    {
        private ApplicationDbContext db;

        public PartiesController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(db.Parties.Select(p => new PartyGet(p)));
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var party = db.Parties.FirstOrDefault(p => p.Id == id);

            if (party == null)
                return NotFound();

            return Ok(new PartyGet(party));
        }
    }
}