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
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Data;
using ParliamentVotes.Managers.DataImport;
using ParliamentVotes.Models.Legislation;
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
        private readonly ApplicationDbContext db;
        private readonly HansardImportManager hansardImportManager;
        private readonly MembersImportManager membersImportManager;
        private readonly BillImportManager billImportManager;
        private readonly SopImportManager sopImportManager;

        public DataImportController(ApplicationDbContext db, HansardImportManager hansardImportManager, MembersImportManager membersImportManager, BillImportManager billImportManager, SopImportManager sopImportManager)
        {
            this.db = db;
            this.hansardImportManager = hansardImportManager;
            this.membersImportManager = membersImportManager;
            this.billImportManager = billImportManager;
            this.sopImportManager = sopImportManager;
        }

        [HttpGet("mps")]
        public async Task<IActionResult> ImportMPsAndParties()
        {
            await membersImportManager.ImportMembers();

            return Ok();
        }

        [HttpGet("questions/by-hansard-url")]
        public async Task<IActionResult> QuestionByHansardDay(string url)
        {
            await hansardImportManager.ImportFromHansard(url);

            return Ok();
        }

        [HttpGet("questions/parliament/{parliamentNumber:int}")]
        public async Task<IActionResult> QuestionsByCurrentParliament(int parliamentNumber)
        {
            await hansardImportManager.GetByParliament(parliamentNumber);

            return Ok();
        }

        [HttpGet("questions/differential")]
        public async Task<IActionResult> QuestionsDifferential()
        {
            await hansardImportManager.GetDifferential();

            return Ok();
        }

        [HttpGet("legislation/bills/by-number")]
        public async Task<IActionResult> ImportSpecificBill(BillType billType, int year, string number)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var bills = await billImportManager.ImportByBillNumber(billType, year, number, context, db.Members.ToList(), db.Parliaments.ToList());
            db.Bills.AddRange(bills);

            return Ok();
        }
        
        [HttpGet("legislation/bills")]
        public async Task<IActionResult> ImportBills()
        {
            
            await billImportManager.ImportAllBills();

            return Ok();
        }

        [HttpGet("legislation/bills/differential")]
        public async Task<IActionResult> BillsDifferential()
        {

            await billImportManager.CheckNewBills();

            return Ok();
        }

        [HttpGet("legislation/sops/by-number")]
        public async Task<IActionResult> ImportSpecificSop(SupplementaryOrderPaperType sopType, int year, string number)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var sops = await sopImportManager.ImportBySopNumber(sopType, year, number, context, db.Members.ToList(), db.Bills.Include(b => b.Parliaments).ToList(), db.Parliaments.ToList());
            db.SupplementaryOrderPapers.AddRange(sops);

            return Ok();
        }

        [HttpGet("legislation/sops")]
        public async Task<IActionResult> ImportSops()
        {

            await sopImportManager.ImportAllSops();

            return Ok();
        }

    }

}