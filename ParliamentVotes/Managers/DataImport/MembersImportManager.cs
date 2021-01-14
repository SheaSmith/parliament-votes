using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using ParliamentVotes.Data;
using ParliamentVotes.Models.Organisational;

namespace ParliamentVotes.Managers.DataImport
{
    public class MembersImportManager
    {
        private readonly ApplicationDbContext _db;

        public MembersImportManager(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ImportMembers()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var allCurrentMpsDocument =
                await context.OpenAsync("https://www.parliament.nz/en/MpBioListingAjax/CurrentListing/13825");

            var currentMps = allCurrentMpsDocument.QuerySelectorAll(".list__cell-body")
                .Where(n => n.Children.Any(c => c.ClassList.Contains("list__cell-heading")));

            var formerMpsDocument =
                await context.OpenAsync(
                    "https://www.parliament.nz/en/mps-and-electorates/former-members-of-parliament/");

            var formerMps = formerMpsDocument.QuerySelectorAll(".list__cell-body")
                .Where(n => n.Children.Any(c => c.ClassList.Contains("list__cell-heading"))).ToList();

            var mps = currentMps.ToList();
            mps.AddRange(formerMps);

            List<Party> parties = _db.Parties.ToList();

            List<Member> members = _db.Members.ToList();
            List<Tenure> tenures = _db.Tenures.ToList();

            List<Member> membersToAdd = new List<Member>();
            List<Tenure> tenuresToAdd = new List<Tenure>();

            var tasks = mps.Select(async mp =>
            {
                if (!await ProcessMpsProfile(mp.QuerySelector("a.list__cell-heading").GetAttribute("href"), context,
                    members, tenures, tenuresToAdd,
                    membersToAdd, parties))
                {
                    ProcessLegacyMpsProfile(mp, members, tenures, tenuresToAdd, membersToAdd, parties);
                }
            });

            await Task.WhenAll(tasks);

            _db.UpdateRange(members);
            _db.UpdateRange(tenures);

            await _db.AddRangeAsync(membersToAdd);
            await _db.AddRangeAsync(tenuresToAdd);

            await _db.SaveChangesAsync();
        }

        private void ProcessLegacyMpsProfile(IElement mp, List<Member> members,
            List<Tenure> tenures, List<Tenure> tenuresToAdd, List<Member> membersToAdd, List<Party> parties)
        {
            // Process older MPs profiles
            var descriptionNode = mp.QuerySelector(".list__cell-abstract");

            var name = mp.QuerySelector("a.list__cell-heading").TextContent.Split(", ");
            var firstName = name[1].Trim();
            var lastName = name[0].Trim();

            Member member = members.FirstOrDefault(d => d.FirstName == firstName && d.LastName == lastName);

            if (member == null)
            {
                member = new Member(firstName, lastName);
                membersToAdd.Add(member);
            }

            var memberships = descriptionNode.TextContent.Replace("10 September;", "10 September 1990;").Replace(
                "NZ First Party, 27 July 2002 - 11 August 2005, 15 February 2008 - 8 November 2008",
                "NZ First Party, 27 July 2002 - 11 August 2005; 15 February 2008 - 8 November 2008").Split("; ");
            Party lastParty = null;
            foreach (string membership in memberships)
            {
                string[] partyComponents = membership.Split(", ");

                string dateSection = null;

                if (partyComponents.Length != 1)
                {
                    var party = partyComponents[0];
                    lock (parties)
                    {
                        lastParty = parties.FirstOrDefault(
                            p => p.Name == party || (p.AlsoKnownAs != null && p.AlsoKnownAs.Contains(party)));

                        if (lastParty == null)
                        {
                            lastParty = new Party(party);
                            parties.Add(lastParty);
                        }
                    }

                    dateSection = partyComponents[1];
                }
                else
                {
                    dateSection = partyComponents[0];
                }

                TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

                var dateComponents = dateSection.Split("-");
                DateTime begin = DateTime.ParseExact(dateComponents[0].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                begin = TimeZoneInfo.ConvertTimeToUtc(begin, nzst);
                DateTime end = DateTime.ParseExact(dateComponents[1].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                end = TimeZoneInfo.ConvertTimeToUtc(end, nzst);

                Tenure tenure = tenures.FirstOrDefault(t => t.Member_Id == member.Id && t.Start == begin);

                if (tenure == null)
                {
                    tenure = new Tenure();
                    tenuresToAdd.Add(tenure);
                }

                tenure.Update(member, begin, lastParty, null, end);
            }
        }

        private async Task<bool> ProcessMpsProfile(string href, IBrowsingContext context, List<Member> members,
            List<Tenure> tenures, List<Tenure> tenuresToAdd, List<Member> membersToAdd, List<Party> parties)
        {
            var mpDocument = await context.OpenAsync("https://www.parliament.nz" + href);

            var tables = mpDocument.QuerySelectorAll("table");

            if (tables.Length == 0)
            {
                return false;
            }

            var name = mpDocument.QuerySelector("title").TextContent.Split(" - ")[0].Trim().Split(", ");

            var firstName = name[1].Trim();
            var lastName = name[0].Trim();

            Member member = members.FirstOrDefault(d => d.FirstName == firstName && d.LastName == lastName);

            if (member == null)
            {
                member = new Member(firstName, lastName);
                membersToAdd.Add(member);
            }

            foreach (var table in tables)
            {
                var firstRow = table.QuerySelector("thead td") ?? table.QuerySelector("thead th");

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

                        Party party = null;

                        lock (parties)
                        {
                            party = parties.FirstOrDefault(
                                p => p.Name == partyName ||
                                     (p.AlsoKnownAs != null && p.AlsoKnownAs.Contains(partyName)));

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

                        var beginString = cells[2].TextContent.Trim();

                        DateTime begin;
                        TimeZoneInfo nzst;
                try
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                }
                catch (TimeZoneNotFoundException)
                {
                    nzst = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
                }

                        if (beginString.Contains("-"))
                        {
                            var dates = beginString.Split("-");
                            begin = DateTime.ParseExact(dates[0].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                            begin = TimeZoneInfo.ConvertTimeToUtc(begin, nzst);
                            end = DateTime.ParseExact(dates[1].Trim(), "d MMMM yyyy", new CultureInfo("en-NZ"));
                            end = TimeZoneInfo.ConvertTimeToUtc(end.Value, nzst);
                        }
                        else
                        {
                            begin = DateTime.Parse(beginString, new CultureInfo("en-NZ"));
                            begin = TimeZoneInfo.ConvertTimeToUtc(begin, nzst);
                        }

                        Tenure tenure = tenures.FirstOrDefault(t => t.Member_Id == member.Id && t.Start == begin);

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

            return true;
        }
    }
}