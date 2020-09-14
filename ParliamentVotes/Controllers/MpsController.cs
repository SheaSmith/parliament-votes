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
    public class MpsController : ControllerBase
    {
        private ApplicationDbContext db;

        public MpsController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(db.Members.Include(m => m.Tenures).ThenInclude(t => t.Party).Select(m => new MemberGet(m)));
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var member = db.Members.Include(m => m.Tenures).ThenInclude(t => t.Party).FirstOrDefault(m => m.Id == id);

            if (member == null)
                return NotFound();

            return Ok(new MemberGet(member));
        }
    }
}