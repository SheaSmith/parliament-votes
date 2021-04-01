using ParliamentVotes.Models.Legislation;
using ParliamentVotes.Models.Organisational;
using ParliamentVotes.Models.Votes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Motions
{
    public class Question
    {
        /// <summary>
        /// An auto generated ID for this question
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The title of the question, e.g. COVID-19 Public Health Response Amendment Bill
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// The title of the question, e.g. First Reading
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// A description of the question, e.g. That the COVID-19 Public Health Response Amendment Bill be now read a first time
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// When was this question considered
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// What type of question was being voted on
        /// </summary>
        [Required]
        public QuestionType QuestionType { get; set; }

        /// <summary>
        /// What stage is this bill on (if applicable)
        /// </summary>
        public Stage? Stage { get; set; }

        /// <summary>
        /// Ammendment clause (if applicable)
        /// </summary>
        public string Clause { get; set; }

        /// <summary>
        /// Whether this question represents a viewpoint generally supported by the conservative members of the house
        /// </summary>
        public bool? PersonalVoteConservativeViewPoint { get; set; }

        /// <summary>
        /// The member who moved this question
        /// </summary>
        public int? Member_Id { get; set; }

        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// The bill this question is associated with
        /// </summary>
        public int? Bill_Id { get; set; }

        [ForeignKey("Bill_Id")]
        public virtual Bill Bill { get; set; }

        /// <summary>
        /// The ID of the SOP this question is associated with
        /// </summary>
        public int? SupplementaryOrderPaper_Id { get; set; }

        [ForeignKey("SupplementaryOrderPaper_Id")]
        public virtual SupplementaryOrderPaper SupplementaryOrderPaper { get; set; }

        /// <summary>
        /// The parliament that this question was discussed under
        /// </summary>
        [Required]
        public int Parliament_Number { get; set; }

        [ForeignKey("Parliament_Number")]
        public virtual Parliament Parliament { get; set; }


        public virtual IEnumerable<PartyVote> PartyVotes { get; set; }
        public virtual IEnumerable<PersonalVote> PersonalVotes { get; set; }
        public virtual IEnumerable<VoiceVote> VoiceVotes { get; set; }
    }
}
