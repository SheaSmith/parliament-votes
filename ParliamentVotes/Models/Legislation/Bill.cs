using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Legislation
{
    public class Bill
    {
        /// <summary>
        /// The ID of the bill
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The title of the bill, e.g. Fuel Industry Bill
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// A description of this bill
        /// </summary>
        [Required]
        public string Description { get; set; }
        
        /// <summary>
        /// A session unique number for this bill
        /// </summary>
        [Required]
        public string BillNumber { get; set; }

        /// <summary>
        /// When this bill was last updated in our database
        /// </summary>
        [Required]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The member that is in charge of this bill
        /// </summary>
        [Required]
        public int Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// What type of bill this is
        /// </summary>
        [Required]
        public BillType Type { get; set; }

        /// <summary>
        /// A slug for this version, which will lead us to the filename
        /// </summary>
        [Required]
        public string DirectoryUrl { get; set; }

        public Bill() {}
        
        public Bill(string title, string description, string billNumber, Member member, BillType billType,
            string directoryUrl)
        {
            Title = title;
            Description = description;
            BillNumber = billNumber;
            Member = member;
            Type = billType;
            DirectoryUrl = directoryUrl;
            LastUpdated = DateTime.UtcNow;
        }
    }

    public enum BillType
    {
        Government,
        Members,
        Private,
        Local
    }
}
