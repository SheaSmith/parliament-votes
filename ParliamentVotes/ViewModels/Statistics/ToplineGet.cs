using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Statistics
{
    public class ToplineGet
    {
        public int Votes { get; set; }

        public int PartyVotes { get; set; }

        public int VoiceVotes { get; set; }

        public int PersonalVotes { get; set; }

        public int Members { get; set; }

        public int Parliaments { get; set; }

        public int Years { get; set; }

        public int Parties { get; set; }
    }
}
