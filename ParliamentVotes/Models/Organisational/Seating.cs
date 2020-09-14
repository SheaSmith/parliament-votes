using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    /// <summary>
    /// A class which defines the seating arrangements for each member
    /// </summary>
    public class Seating
    {
        /// <summary>
        /// The parliament that this seat applies too
        /// </summary>
        public int Parliament_Number { get; set; }

        [ForeignKey("Parliament_Number")]
        public virtual Parliament Parliament { get; set; }

        /// <summary>
        /// The member we are defining the seat for
        /// </summary>
        public int Member_Id { get; set; }
        
        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// The index of the seat that this member belongs too
        /// </summary>
        public int SeatIndex { get; set; }
    }
}
