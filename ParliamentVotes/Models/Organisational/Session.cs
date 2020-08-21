using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class Session
    {
        /// <summary>
        /// The number for this session, e.g. for the 51st parliament it would be 51
        /// </summary>
        [Key, Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SessionNumber { get; set; }

        /// <summary>
        /// The start of this session
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end of this session (if applicable)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// A list of the governing parties for this session
        /// </summary>
        public virtual List<GoverningParty> GoverningParties { get; set; }
    }
}
