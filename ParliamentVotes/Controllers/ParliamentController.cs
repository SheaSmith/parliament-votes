using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Data;
using ParliamentVotes.ViewModels;
using ParliamentVotes.ViewModels.Organisational;

namespace ParliamentVotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParliamentController : ControllerBase
    {
        private ApplicationDbContext db;

        public ParliamentController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(db.Parliaments.Include(s => s.GoverningParties).ThenInclude(g => g.Party).Select(s => new ParliamentGet(s)));
        }

        [HttpGet("{number}")]
        public IActionResult GetByNumber(int number)
        {
            var parliament = db.Parliaments.Include(s => s.GoverningParties).ThenInclude(g => g.Party).FirstOrDefault(s => s.Number == number);

            if (parliament == null)
                return NotFound();

            return Ok(new ParliamentGet(parliament));
        }
    }
}