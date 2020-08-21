using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class TenureGetModel
    {
        public PartyGetModel Party { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Electorate { get; set; }

        public TenureGetModel(Tenure tenure)
        {
            Party = new PartyGetModel(tenure.Party);
            Start = tenure.Start;
            End = tenure.End;
            Electorate = tenure.Electorate;
        }
    }
}
