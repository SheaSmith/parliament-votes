using ParliamentVotes.Models.Motions;
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
        /// The year for this SOP
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// The date this SOP was submitted
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// The parliament this SOP was submitted under
        /// </summary>
        [Required]
        public int Parliament_Number { get; set; }

        [ForeignKey("Parliament_Number")]
        public Parliament Parliament { get; set; }

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
        /// The type of this SOP
        /// </summary>
        [Required]
        public SupplementaryOrderPaperType Type { get; set; }

        /// <summary>
        /// A slug for this version, which will lead us to the filename
        /// </summary>
        [Required]
        public string DirectoryUrl { get; set; }

        /// <summary>
        /// The questions associated with this SOP
        /// </summary>
        public virtual List<Question> Questions { get; set; }

        public SupplementaryOrderPaper() { }

        public SupplementaryOrderPaper(int number, int year, Member member, Bill amendingBill, SupplementaryOrderPaperType type, string directoryUrl, Parliament parliament, DateTime date)
        {
            Number = number;
            Year = year;
            Member = member;
            AmendingBill = amendingBill;
            Type = type;
            DirectoryUrl = directoryUrl;
            Parliament = parliament;
            Date = date;
        }

    }

    public enum SupplementaryOrderPaperType
    {
        Government,
        Members
    }
}
