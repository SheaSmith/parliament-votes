using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class SessionGetModel
    {
        public int SessionNumber { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public List<GoverningPartyGetModel> GoverningParties { get; set; }

        public SessionGetModel(Session session)
        {
            SessionNumber = session.SessionNumber;
            Start = session.StartDate;
            End = session.EndDate;
            GoverningParties = session.GoverningParties.Select(g => new GoverningPartyGetModel(g)).ToList();
        }
    }
}
