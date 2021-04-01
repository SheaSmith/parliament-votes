using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Organisational
{
    public class PartyGet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Colour { get; set; }

        public PartyGet(Party party)
        {
            Id = party.Id;
            Name = party.Name;
            LogoUrl = party.LogoUrl;
            Colour = party.Color;
        }
    }
}
