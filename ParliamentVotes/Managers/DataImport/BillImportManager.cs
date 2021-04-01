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

namespace ParliamentVotes.Managers.DataImport
{
    public class BillImportManager
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;

        public BillImportManager(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            this.db = db;
            this.webHostEnvironment = webHostEnvironment;
        }


        public async Task ImportAllBills()
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var billTypesDocument = await context.OpenAsync("http://legislation.govt.nz/subscribe/bill");

            var linkNodes = billTypesDocument.QuerySelectorAll(".directory a");

            foreach (var linkNode in linkNodes)
            {
                BillType billType = linkNode.TextContent switch
                {
                    "local" => BillType.Local,
                    "member" => BillType.Members,
                    "private" => BillType.Private,
                    _ => BillType.Government
                };

                await ImportBillsByType(billType, context);
            }
        }

        private async Task ImportBillsByType(BillType billType, IBrowsingContext context)
        {
            string url = billType switch
            {
                BillType.Local => "http://legislation.govt.nz/subscribe/bill/local",
                BillType.Members => "http://legislation.govt.nz/subscribe/bill/member",
                BillType.Private => "http://legislation.govt.nz/subscribe/bill/private",
                _ => "http://legislation.govt.nz/subscribe/bill/government"
            };

            var yearsDocument = await context.OpenAsync(url);

            var yearNodes = yearsDocument.QuerySelectorAll(".directory a");

            foreach (var yearNode in yearNodes)
            {
                int year = int.Parse(yearNode.TextContent);
                try
                {
                    var donePath = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                            year.ToString(), string.Format("{0}-done.txt", billType));

                    if (File.Exists(donePath))
                        continue;
                }
                catch (DirectoryNotFoundException)
                {
                    // do nothing
                }

                await ImportBillsByYearAndType(billType, year, context);
            }
        }

        private async Task ImportBillsByYearAndType(BillType billType, int year, IBrowsingContext context)
        {
            string url = billType switch
            {
                BillType.Local => "http://legislation.govt.nz/subscribe/bill/local/",
                BillType.Members => "http://legislation.govt.nz/subscribe/bill/member/",
                BillType.Private => "http://legislation.govt.nz/subscribe/bill/private/",
                _ => "http://legislation.govt.nz/subscribe/bill/government/"
            } + year;

            var billsDocument = await context.OpenAsync(url);

            var billNodes = billsDocument.QuerySelectorAll(".directory a");

            var bills = new List<Bill>();

            SemaphoreSlim maxThread = new SemaphoreSlim(10);

            var members = db.Members.ToList();
            var parliaments = db.Parliaments.ToList();

            var allTasks = new List<Task>();

            foreach (var billNode in billNodes)
            {
                maxThread.Wait();

                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string billNumber = billNode.TextContent;

                            bills.AddRange(await ImportByBillNumber(billType, year, billNumber, context, members, parliaments));
                        }
                        finally
                        {
                            maxThread.Release();
                        }
                    }
                ));
            }

            await Task.WhenAll(allTasks);

            db.Bills.AddRange(bills);
            await db.SaveChangesAsync();

            var donePath = Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                        year.ToString(), string.Format("{0}-done.txt", billType));
            File.Create(donePath);
        }

        public async Task<List<Bill>> ImportByBillNumber(BillType billType, int year, string billNumber, IBrowsingContext context, List<Member> members, List<Parliament> parliaments)
        {
            string url =
                $"{billType switch { BillType.Local => "http://legislation.govt.nz/subscribe/bill/local/", BillType.Members => "http://legislation.govt.nz/subscribe/bill/member/", BillType.Private => "http://legislation.govt.nz/subscribe/bill/private/", _ => "http://legislation.govt.nz/subscribe/bill/government/" }}/{year}/{billNumber}";

            var versionsDocument = await context.OpenAsync(url);

            var versionsNodes = versionsDocument.QuerySelectorAll(".directory a");

            var bills = new List<Bill>();

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

                        string billTitle = legislationXml.XPathSelectElement("//cover//title").Value;

                        string billTypeSlug = billType switch
                        {
                            BillType.Local => "local",
                            BillType.Members => "member's",
                            BillType.Private => "private",
                            _ => "government"
                        };

                        string description = "";

                        string billNumberFormatted = legislationXml.XPathSelectAttributeValue("//bill/@bill.no");
                        string billSplitFormatted = legislationXml.XPathSelectAttributeValue("//bill/@split.letter");

                        try
                        {

                            string descriptionUrl =
                                $"https://www.parliament.nz/en/ajax/billslisting/1323/all?Criteria.PageNumber=1&Criteria.Keyword=\"{billTitle}\"&Criteria.ViewDetails=1";

                            // Get a description for this bill
                            var descriptionDocument = await context.OpenAsync(descriptionUrl);

                            var tableRow = descriptionDocument.QuerySelectorAll(".list__row > td")
                                .FirstOrDefault(d =>
                                    d.QuerySelectorAll("tr").FirstOrDefault(r => r.QuerySelector("th").TextContent.Contains("Bill no")) != null &&
                                    d.QuerySelectorAll("tr").FirstOrDefault(r => r.QuerySelector("th").TextContent.Contains("Bill no")).QuerySelector("td").TextContent.Trim().StartsWith(billNumberFormatted) && 
                                    d.QuerySelectorAll("tr").FirstOrDefault(r => r.QuerySelector("th").TextContent.Contains("Bill no")).QuerySelector("td").TextContent.Trim().EndsWith(billSplitFormatted)
                                 );

                            if (tableRow == null)
                                return bills;

                            var descriptionNode = tableRow.QuerySelector(".section").ChildNodes
                                    .FirstOrDefault(c => c.NodeType == NodeType.Text && c.TextContent.Trim() != "");


                            description = descriptionNode != null
                                ? descriptionNode.TextContent.Trim()
                                : "";

                            var parlimentNumbers = tableRow.QuerySelectorAll("tr").FirstOrDefault(r => r.QuerySelector("th").TextContent.Contains("Parliament")).QuerySelector("td").TextContent.Split("-");

                            int fromParliament = Convert.ToInt32(parlimentNumbers[0]);
                            int toParliament = fromParliament;

                            if (parlimentNumbers.Length == 2)
                                toParliament = Convert.ToInt32(parlimentNumbers[1]);

                            var billParliaments = parliaments.Where(p => p.Number >= fromParliament && p.Number <= toParliament).ToList();

                            try
                            {
                                if (description == "")
                                    description = tableRow == null || tableRow.QuerySelector(".section p") == null
                                        ? ""
                                        : tableRow.QuerySelector(".section p").TextContent.Trim();
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }

                            string memberName = legislationXml.XPathSelectElement("//cover//member").Value;
                            memberName = memberName
                                .Replace("Rt Hon ", "")
                                .Replace("Rt Hon. ", "")
                                .Replace("Hon ", "")
                                .Replace("Hon. ", "")
                                .Replace("Dr ", "")
                                .Replace("Sir ", "")
                                .Replace("Dame ", "")
                                .Replace("Vui ", "")
                                .Replace("Luamanuvao ", "")
                                .Replace("’", "'")
                                .Replace("`", "'")
                                .Trim();

                            List<Member> member = null;

                            var memberNames = memberName.Replace(", and", ",").Split(", ");

                            member = members.Where(m => memberNames.Contains(m.FirstName + " " + m.LastName) || memberNames.Any(name => (m.AlsoKnownAs != null && m.AlsoKnownAs.Contains(name)))).ToList();

                            string formattedBillNumber = string.Format("{0}{1}", billNumberFormatted, billSplitFormatted);

                            Bill bill = new Bill(billTitle, description, formattedBillNumber, member, billType, versionUrl, billParliaments, year);
                            bills.Add(bill);

                            Directory.CreateDirectory(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                                year.ToString()));
                            await File.WriteAllTextAsync(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                                year.ToString(), $"{formattedBillNumber}.xml"), xml);
                        }
                        catch (Exception e)
                        {
                            int i = 0;
                            // do nothing
                        }

                        
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    break;
                }
            }

            return bills;
        }

        public async Task CheckNewBills()
        {

            var lastBill = db.Bills.OrderByDescending(b => b.LastUpdated).Select(b => b.LastUpdated).FirstOrDefault();

            var days = (DateTime.Today - lastBill.Date).Days;
            days = days > 90 ? 90 : days;

            string xml;
            using (var webClient = new WebClient())
            {
                xml = webClient.DownloadString(
                    $"https://legislation.govt.nz/atom.aspx?search=ta_bill_All_bc@bcur_an@bn@rn_25_a&p=1&t=Bills&d={days}");
            }

            XDocument legislationXml = XDocument.Parse(xml);

            var newBills = legislationXml.XPathSelectElements("//entry");

            var members = db.Members.ToList();
            var parliaments = db.Parliaments.ToList();

            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);

            foreach (var newBill in newBills)
            {
                var linkComponents = newBill.XPathSelectElement("//link").Attribute("href").Value.Split("/");

                BillType type = linkComponents[2] switch
                {
                    "local" => BillType.Local,
                    "member" => BillType.Members,
                    "private" => BillType.Private,
                    _ => BillType.Government
                };

                var year = Convert.ToInt32(linkComponents[3]);
                var billNumber = linkComponents[4];

                var bills = await ImportByBillNumber(type, year, billNumber, context, members, parliaments);
                db.Bills.AddRange(bills);
            }
        }
    }
}