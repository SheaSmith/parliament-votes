using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class GoverningPartyGet
    {
        public PartyGet Party { get; set; }
        public GoverningRelationship? Relationship { get; set; }

        public GoverningPartyGet(GoverningParty governingParty)
        {
            Party = new PartyGet(governingParty.Party);
            Relationship = governingParty.Relationship;
        }
    }
}
