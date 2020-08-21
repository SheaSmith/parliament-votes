using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class PartyGetModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Color { get; set; }

        public PartyGetModel(Party party)
        {
            Id = party.Id;
            Name = party.Name;
            LogoUrl = party.LogoUrl;
            Color = party.Color;
        }
    }
}
