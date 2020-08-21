using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Data;
using ParliamentVotes.ViewModels;

namespace ParliamentVotes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        private ApplicationDbContext db;

        public SessionsController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(db.Sessions.Include(s => s.GoverningParties).ThenInclude(g => g.Party).Select(s => new SessionGetModel(s)));
        }

        [HttpGet("{number}")]
        public IActionResult GetByNumber(int number)
        {
            var session = db.Sessions.Include(s => s.GoverningParties).ThenInclude(g => g.Party).FirstOrDefault(s => s.SessionNumber == number);

            if (session == null)
                return NotFound();

            return Ok(new SessionGetModel(session));
        }
    }
}