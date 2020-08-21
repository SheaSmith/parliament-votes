using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Votes
{
    public class SplitPartyVote
    {
        public SplitPartyVote() { }
        public SplitPartyVote(PartyVote partyVote, Member member)
        {
            PartyVote = partyVote;
            Member = member;
        }

        /// <summary>
        /// The party vote this split is associated with
        /// </summary>
        [Required]
        public int PartyVote_Id { get; set; }

        [ForeignKey("PartyVote_Id")]
        public virtual PartyVote PartyVote { get; set; }

        /// <summary>
        /// The member that voted for this party vote position
        /// </summary>
        [Required]
        public int Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }
    }
}
