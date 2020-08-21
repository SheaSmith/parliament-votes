using ParliamentVotes.Models.Motions;
using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Votes
{
    public class PartyVote : Vote
    {

        public void Update(Question question, int numberOfVotes, bool? position = null, Party party = null, Member member = null, string complexPosition = null, List<SplitPartyVote> splitPartyVotes = null)
        {
            base.Update(question, position, complexPosition);
            NumberOfVotes = numberOfVotes;
            Party = party;
            Member = member;
            SplitPartyVotes = splitPartyVotes;
        }

        /// <summary>
        /// The ID of the party making this vote. This is not required as there may be independant MPs voting in a party vote
        /// </summary>
        public int? Party_Id { get; set; }

        [ForeignKey("Party_Id")]
        public virtual Party Party { get; set; }

        /// <summary>
        /// The ID of the member making this vote, e.g. if they are an independent
        /// </summary>
        public int? Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }


        /// <summary>
        /// The number of votes cast with this party vote. This may be less than the total membership due to proxy voting rules, or due to a split party vote
        /// </summary>
        [Required]
        public int NumberOfVotes { get; set; }

        /// <summary>
        /// If this is a split party vote, specify which members voted for this position
        /// </summary>
        public virtual List<SplitPartyVote> SplitPartyVotes { get; set; }
    }
}
