using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ParliamentVotes.Extensions
{
    public static class XDocumentExtensions
    {
        public static string XPathSelectAttributeValue(this XDocument element, string xPath)
        {
            try
            {
                return ((IEnumerable<object>) element.XPathEvaluate(xPath)).OfType<XAttribute>().First().Value;
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }
}