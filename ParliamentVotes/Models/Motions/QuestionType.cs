using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Motions
{
    public enum QuestionType
    {
        /// <summary>
        /// A motion, e.g. a motion to accord urgency
        /// </summary>
        Motion,
        /// <summary>
        /// A reading of a bill, so first third or second
        /// </summary>
        BillReading,
        /// <summary>
        /// A supplementary order paper (e.g. through committee of the whole)
        /// </summary>
        SupplementaryOrderPaper,
        /// <summary>
        /// A part of a bill, e.g. specific sections, clauses or schedules
        /// </summary>
        BillPart,
        /// <summary>
        /// An amendment moved outside of the SOP process
        /// </summary>
        Amendment
    }
}
