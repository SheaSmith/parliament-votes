using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Organisational
{
    public class ParliamentGet
    {
        public int Number { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public List<GoverningPartyGet> GoverningParties { get; set; }

        public ParliamentGet(Parliament parliament)
        {
            Number = parliament.Number;
            Start = parliament.StartDate;
            End = parliament.EndDate;
            GoverningParties = parliament.GoverningParties.Select(g => new GoverningPartyGet(g)).ToList();
        }
    }
}
