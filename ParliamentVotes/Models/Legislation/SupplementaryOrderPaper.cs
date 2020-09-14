using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Legislation
{
    public class SupplementaryOrderPaper
    {
        /// <summary>
        /// The ID of the SOP
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The number of this SOP, e.g. 569
        /// </summary>
        [Required]
        public int Number { get; set; }

        /// <summary>
        /// When this SOP was last updated in the database
        /// </summary>
        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The parliament of this SOP
        /// </summary>
        [Required]
        public int Parliament_Number { get; set; }

        [ForeignKey("Parliament_Number")]
        public virtual Parliament Parliament { get; set; }

        /// <summary>
        /// The ID of the member who is in charge of this SOP
        /// </summary>
        [Required]
        public int Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// The ID of the bill that is being amended (if applicable)
        /// </summary>
        public int? AmendingBill_Id { get; set; }

        [ForeignKey("AmendingBill_Id")]
        public virtual Bill AmendingBill { get; set; }

        /// <summary>
        /// The ID of the SOP that is being ammended (if applicable)
        /// </summary>
        public int? AmendingSupplementaryOrderPaper_Id { get; set; }

        [ForeignKey("AmendingSupplementaryOrderPaper_Id")]
        public virtual SupplementaryOrderPaper AmendingSupplementaryOrderPaper { get; set; }

        /// <summary>
        /// The type of this SOP
        /// </summary>
        [Required]
        public SupplementaryOrderPaperType Type { get; set; }

        /// <summary>
        /// A slug for this version, which will lead us to the filename
        /// </summary>
        [Required]
        public string Slug { get; set; }

        /// <summary>
        /// The name of the XML file for this SOP
        /// </summary>
        [Required]
        public string FileName { get; set; }

    }

    public enum SupplementaryOrderPaperType
    {
        Government,
        Members
    }
}
