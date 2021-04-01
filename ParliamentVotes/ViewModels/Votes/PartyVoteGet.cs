using ParliamentVotes.Models.Votes;
using ParliamentVotes.ViewModels.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Votes
{
    public class PartyVoteGet : VoteGet
    {
        public PartyGet Party { get; set; }

        public MemberGet Member { get; set; }

        public int NumberOfVotes { get; set; }

        public IEnumerable<MemberGet> SplitPartyVotes { get; set; }

        public PartyVoteGet() : base() { }

        public PartyVoteGet(PartyVote vote) : base(vote)
        {
            Party = vote.Party == null ? null : new PartyGet(vote.Party);
            Member = vote.Member == null ? null : new MemberGet(vote.Member);

            if (Party == null && Member == null)
                throw new Exception("Both party and member cannot be null");

            NumberOfVotes = vote.NumberOfVotes;
            SplitPartyVotes = vote.SplitPartyVotes.Count() == 0 ? null : vote.SplitPartyVotes.Select(s => new MemberGet(s));
        }
    }
}
