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

namespace ParliamentVotes.Managers.DataImport
{
    public class LegislationImportManager
    {
        private readonly ApplicationDbContext db;
        private readonly IWebHostEnvironment webHostEnvironment;

        public LegislationImportManager(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
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

            foreach (var billNode in billNodes)
            {
                string billNumber = billNode.TextContent;

                await ImportByBillNumber(billType, year, billNumber, context);
            }
        }

        public async Task ImportByBillNumber(BillType billType, int year, string billNumber, IBrowsingContext context)
        {
            string url =
                $"{billType switch {BillType.Local => "http://legislation.govt.nz/subscribe/bill/local/", BillType.Members => "http://legislation.govt.nz/subscribe/bill/member/", BillType.Private => "http://legislation.govt.nz/subscribe/bill/private/", _ => "http://legislation.govt.nz/subscribe/bill/government/"}}/{year}/{billNumber}";

            var versionsDocument = await context.OpenAsync(url);

            var versionsNodes = versionsDocument.QuerySelectorAll(".directory a");

            foreach (var versionNode in versionsNodes)
            {
                string version = versionNode.TextContent;

                string versionUrl = $"{url}/{version}";
                var versionDocument = await context.OpenAsync(versionUrl);

                if (versionDocument.StatusCode == HttpStatusCode.OK)
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

                    string billTitle = legislationXml.XPathSelectElement("//billdetail//title").Value;

                    string billTypeSlug = billType switch
                    {
                        BillType.Local => "local", BillType.Members => "member's", BillType.Private => "private",
                        _ => "government"
                    };
                    string descriptionUrl =
                        $"https://www.parliament.nz/en/ajax/billslisting/1323/all?Criteria.PageNumber=1&Criteria.Keyword=\"{billTitle}\"&Criteria.Timeframe=range&Criteria.DateFrom={year}-01-01&Criteria.DateTo={year}-12-31&Criteria.Dt=Bill - {billTypeSlug}&Criteria.ViewDetails=1";

                    // Get a description for this bill
                    var descriptionDocument = await context.OpenAsync(descriptionUrl);

                    var tableRow = descriptionDocument.QuerySelectorAll(".list__row td")
                        .FirstOrDefault(d =>
                            (d.QuerySelector("h2") != null && d.QuerySelector("h2").TextContent == billTitle) ||
                            (d.QuerySelector("h1") != null && d.QuerySelector("h1").TextContent == billTitle));

                    string description = tableRow != null
                        ? tableRow.QuerySelector(".section").ChildNodes
                            .FirstOrDefault(c => c.NodeType == NodeType.Text && c.TextContent.Trim() != "").TextContent.Trim()
                        : "";

                    try
                    {
                        if (description == "")
                            description = tableRow == null
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
                        .Replace("Hon ", "")
                        .Replace("Dr ", "")
                        .Replace("Sir ", "")
                        .Replace("Dame ", "")
                        .Replace("Vui ", "")
                        .Replace("’", "'")
                        .Trim();

                    var member = db.Members.FirstOrDefault(m => m.FirstName + " " + m.LastName == memberName);

                    if (member == null && year > 2005)
                        throw new Exception("Member not found. Name " + memberName);

                    string formattedBillNumber = legislationXml.XPathSelectAttributeValue("//bill/@bill.no") +
                                                 legislationXml.XPathSelectAttributeValue("//bill/@split.letter");

                    Bill bill = new Bill(billTitle, description, formattedBillNumber, member, billType, versionUrl);
                    await db.Bills.AddAsync(bill);

                    Directory.CreateDirectory(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                        year.ToString()));
                    await File.WriteAllTextAsync(Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "Bills",
                        year.ToString(), $"{formattedBillNumber}.xml"), xml);

                    break;
                }
            }

            await db.SaveChangesAsync();
        }
    }
}