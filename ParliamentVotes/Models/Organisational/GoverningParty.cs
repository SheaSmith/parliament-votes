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
        /// The number of the parliament
        /// </summary>
        [Required]
        public int Parliament_Number { get; set; }
        [ForeignKey("Parliament_Number")]
        public virtual Parliament Parliament { get; set; }

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
