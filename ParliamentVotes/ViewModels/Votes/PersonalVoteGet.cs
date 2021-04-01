using ParliamentVotes.Models.Votes;
using ParliamentVotes.ViewModels.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Votes
{
    public class PersonalVoteGet : VoteGet
    {
        public MemberGet Member { get; set; }

        public bool Proxy { get; set; }

        public PersonalVoteGet() : base() { }

        public PersonalVoteGet(PersonalVote vote) : base(vote)
        {
            Member = new MemberGet(vote.Member);
            Proxy = vote.Proxy;
        }
    }
}
