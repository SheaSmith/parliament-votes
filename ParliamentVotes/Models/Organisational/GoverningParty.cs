using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class GoverningParty
    {
        /// <summary>
        /// The number of the parliamentary sessions
        /// </summary>
        [Required]
        public int Session_Number { get; set; }
        [ForeignKey("Session_Number")]
        public virtual Session Session { get; set; }

        /// <summary>
        /// The party that is part of the government
        /// </summary>
        [Required]
        public int Party_Id { get; set; }
        [ForeignKey("Party_Id")]
        public virtual Party Party { get; set; }

        /// <summary>
        /// What part goes this party have within the government
        /// </summary>
        public GoverningRelationship? Relationship { get; set; }
    }

    public enum GoverningRelationship
    {
        Coalition,
        ConfidenceAndSupply
    }
}
