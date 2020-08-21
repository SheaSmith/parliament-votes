using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class GoverningPartyGetModel
    {
        public PartyGetModel Party { get; set; }
        public GoverningRelationship? Relationship { get; set; }

        public GoverningPartyGetModel(GoverningParty governingParty)
        {
            Party = new PartyGetModel(governingParty.Party);
            Relationship = governingParty.Relationship;
        }
    }
}
