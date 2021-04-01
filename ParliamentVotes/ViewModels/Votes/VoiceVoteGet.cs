using ParliamentVotes.Models.Votes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Votes
{
    public class VoiceVoteGet : VoteGet
    {
        public VoiceVoteGet() : base() { }

        public VoiceVoteGet(VoiceVote vote) : base(vote) { }
    }
}
