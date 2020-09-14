using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class Parliament
    {
        /// <summary>
        /// The number for this parliament, e.g. for the 51st parliament it would be 51
        /// </summary>
        [Key, Required]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Number { get; set; }

        /// <summary>
        /// The start of this parliament
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end of this parliament (if applicable)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// A list of the governing parties for this parliament
        /// </summary>
        public virtual List<GoverningParty> GoverningParties { get; set; }

        /// <summary>
        /// The seating plan for this parliament
        /// </summary>
        public virtual List<Seating> SeatingPlan { get; set; }
    }
}
