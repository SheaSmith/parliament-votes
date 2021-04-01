using ParliamentVotes.Models.Votes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Votes
{
    public abstract class VoteGet
    {
        public int Id { get; set; }

        public bool? Position { get; set; }

        public string ComplexPosition { get; set; }


        public VoteGet() { }

        public VoteGet(Vote vote)
        {
            Id = vote.Id;
            Position = vote.Position;
            ComplexPosition = vote.ComplexPosition;
        }
    }
}
