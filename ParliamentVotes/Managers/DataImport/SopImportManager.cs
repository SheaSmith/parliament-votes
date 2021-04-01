using ParliamentVotes.Data;
using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Hosting;
using ParliamentVotes.Models.Legislation;
using ParliamentVotes.Extensions;
using System.Threading;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace ParliamentVotes.Managers.DataImport
{
    public class SopImportManager
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;

        public SopImportManager(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
        }


        public async Task ImportAllSops()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var sopTypesDocument = await context.OpenAsync("http://legislation.govt.nz/subscribe/sop");

            var linkNodes = sopTypesDocument.QuerySelectorAll(".directory a");

            foreach (var linkNode in linkNodes)
            {
                SupplementaryOrderPaperType sopType = linkNode.TextContent switch
                {
                    "government" => SupplementaryOrderPaperType.Government,
                    "members" => SupplementaryOrderPaperType.Members,
                    _ => SupplementaryOrderPaperType.Government
                };

                await ImportSopsByType(sopType, context);
            }
        }

        private async Task ImportSopsByType(SupplementaryOrderPaperType sopType, IBrowsingContext context)
        {
            string url = sopType switch
            {
                SupplementaryOrderPaperType.Members => "http://legislation.govt.nz/subscribe/sop/members",
                _ => "http://legislation.govt.nz/subscribe/sop/government"
            };

            var yearsDocument = await context.OpenAsync(url);

            var yearNodes = yearsDocument.QuerySelectorAll(".directory a");

            foreach (var yearNode in yearNodes)
            {
                int year = int.Parse(yearNode.TextContent);
                try
                {
                    var donePath = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "SupplementaryOrderPapers",
                            year.ToString(), string.Format("{0}-done.txt", sopType));

                    if (File.Exists(donePath))
                        continue;
                }
                catch (DirectoryNotFoundException)
                {
                    // do nothing
                }

                await ImportSopsByYearAndType(sopType, year, context);
            }
        }

        private async Task ImportSopsByYearAndType(SupplementaryOrderPaperType sopType, int year, IBrowsingContext context)
        {
            string url = sopType switch
            {
                SupplementaryOrderPaperType.Members => "http://legislation.govt.nz/subscribe/sop/members/",
                _ => "http://legislation.govt.nz/subscribe/sop/government/"
            } + year;

            var sopsDocument = await context.OpenAsync(url);

            var sopNodes = sopsDocument.QuerySelectorAll(".directory a");

            var sops = new List<SupplementaryOrderPaper>();

            SemaphoreSlim maxThread = new SemaphoreSlim(10);

            var members = db.Members.ToList();
            var bills = db.Bills.Include(b => b.Parliaments).ToList();
            var parliaments = db.Parliaments.ToList();

            var allTasks = new List<Task>();

            foreach (var sopNode in sopNodes)
            {
                maxThread.Wait();

                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string sopNumber = sopNode.TextContent;

                            sops.AddRange(await ImportBySopNumber(sopType, year, sopNumber, context, members, bills, parliaments));
                        }
                        finally
                        {
                            maxThread.Release();
                        }
                    }
                ));
            }

            await Task.WhenAll(allTasks);

            db.SupplementaryOrderPapers.AddRange(sops);
            await db.SaveChangesAsync();

            var donePath = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "SupplementaryOrderPapers",
                        year.ToString(), string.Format("{0}-done.txt", sopType));
            File.Create(donePath);
        }

        public async Task<List<SupplementaryOrderPaper>> ImportBySopNumber(SupplementaryOrderPaperType sopType, int year, string sopNumber, IBrowsingContext context, List<Member> members, List<Bill> bills, List<Parliament> parliaments)
        {
            string url =
                $"{sopType switch { SupplementaryOrderPaperType.Members => "http://legislation.govt.nz/subscribe/sop/members", _ => "http://legislation.govt.nz/subscribe/sop/government" }}/{year}/{sopNumber}";

            var versionsDocument = await context.OpenAsync(url);

            var versionsNodes = versionsDocument.QuerySelectorAll(".directory a");

            var sops = new List<SupplementaryOrderPaper>();

            foreach (var versionNode in versionsNodes)
            {
                string version = versionNode.TextContent;

                string versionUrl = $"{url}/{version}";
                var versionDocument = await context.OpenAsync(versionUrl);

                if (versionDocument.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        var documentNode = versionDocument.QuerySelectorAll(".file a")
                            .First(f => f.TextContent.EndsWith(".xml"));

                        string xml;
                        using (var webClient = new WebClient())
                        {
                            xml = webClient.DownloadString(
                                $"http://legislation.govt.nz{documentNode.Attributes["href"].Value}");
                        }

                        XDocument legislationXml = XDocument.Parse(xml);

                        string memberName = legislationXml.XPathSelectElement("//motion")?.Value;

                        if (memberName.Contains("Minister"))
                            memberName = legislationXml.XPathSelectElement("//cover//member").Value;

                        memberName = memberName
                            .Replace("The Right Honourable ", "")
                            .Replace("Right Honourable ", "")
                            .Replace("The Rt Honourable ", "")
                            .Replace("The Rt Hon ", "")
                            .Replace("Rt Hon ", "")
                            .Replace("Rt Hon. ", "")
                            .Replace("The Honourable ", "")
                            .Replace("Honourable ", "")
                            .Replace("The Hon ", "")
                            .Replace("Hon ", "")
                            .Replace("Hon. ", "")
                            .Replace("Dr ", "")
                            .Replace("Sir ", "")
                            .Replace("Dame ", "")
                            .Replace("Vui ", "")
                            .Replace("Luamanuvao ", "")
                            .Replace("’", "'")
                            .Replace("`", "'")
                            .Split(",")[0]
                            .Trim();

                        Member member = members.FirstOrDefault(m => m.FirstName + " " + m.LastName == memberName || (m.AlsoKnownAs != null && m.AlsoKnownAs.Contains(memberName)));

                        if (member == null)
                            throw new Exception("Member " + memberName + " not found");

                        var sopDate = DateTime.ParseExact(legislationXml.XPathSelectElement("//sop//date").Value.Trim().Replace(", ", " ").Replace(" ", " "), "dddd d MMMMM yyyy", CultureInfo.InvariantCulture);

                        var parliament = parliaments.FirstOrDefault(p => p.StartDate <= sopDate && (p.EndDate == null || p.EndDate >= sopDate));

                        string amendingBill = legislationXml.XPathSelectElement("//billref").Value.Trim();

                        var bill = bills.FirstOrDefault(b => b.Title == amendingBill && b.Parliaments.Contains(parliament));

                        if (bill == null)
                            Console.WriteLine("bill not found " + amendingBill);


                        int formattedSopNumber = Convert.ToInt32(legislationXml.XPathSelectAttributeValue("//sop/@sop.no"));

                        SupplementaryOrderPaper sop = new SupplementaryOrderPaper(formattedSopNumber, year, member, bill, sopType, versionUrl, parliament, sopDate);
                        sops.Add(sop);

                        Directory.CreateDirectory(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "SupplementaryOrderPapers",
                            year.ToString()));
                        await File.WriteAllTextAsync(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "SupplementaryOrderPapers",
                            year.ToString(), $"{formattedSopNumber}.xml"), xml);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    break;
                }
            }

            return sops;
        }
    }
}