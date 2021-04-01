using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Organisational
{
    public class TenureGet
    {
        public PartyGet Party { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Electorate { get; set; }

        public TenureGet(Tenure tenure)
        {
            Party = tenure.Party == null ? null : new PartyGet(tenure.Party);
            Start = tenure.Start;
            End = tenure.End;
            Electorate = tenure.Electorate;
        }
    }
}
