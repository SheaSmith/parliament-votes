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
using ParliamentVotes.Managers.DataImport;
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

        private readonly ApplicationDbContext db;
        private readonly HansardImportManager hansardImportManager;

        public DataImportController(ApplicationDbContext db, HansardImportManager hansardImportManager)
        {
            this.db = db;
            this.hansardImportManager = hansardImportManager;
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

            return Ok(db.Members.Select(m => new MemberGet(m)));
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
            await hansardImportManager.ImportFromHansard(url);

            return Ok();
        }

        [HttpGet("question/this-parliament")]
        public async Task<IActionResult> CurrentParliament()
        {
            await hansardImportManager.GetUrls();

            return Ok();
        }

    }

}