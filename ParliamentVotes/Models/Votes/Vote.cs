using ParliamentVotes.Models.Motions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Votes
{
    public partial class Vote
    {
        public void Update(Question question, bool? position = null, string complexPosition = null)
        {
            Question = question;
            Position = position;
            ComplexPosition = complexPosition;
        }

        /// <summary>
        /// An auto-generated ID for this vote
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The question associated with this vote
        /// </summary>
        [Required]
        public int Question_Id { get; set; }

        [ForeignKey("Question_Id")]
        public virtual Question Question { get; set; }

        /// <summary>
        /// Whether this vote was in favour, or against. This field is optional, as ComplexPosition may be sometimes set
        /// </summary>
        public bool? Position { get; set; }

        /// <summary>
        /// When there is more than one position that needs considered (e.g. Alcohol Reform Bill), or when voting on something that isn't a simple Ayes/Nays question (e.g. a contested speaker election)
        /// </summary>
        public string ComplexPosition { get; set; }
    }
}
