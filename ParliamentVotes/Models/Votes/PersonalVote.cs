using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Votes
{
    public class PersonalVote : Vote
    {
        /// <summary>
        /// The ID of the member making this vote
        /// </summary>
        [Required]
        public int Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// Whether this vote was a proxy vote
        /// </summary>
        [Required]
        public bool Proxy { get; set; }
    }
}
