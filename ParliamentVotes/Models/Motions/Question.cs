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
        public string QuestionTitle { get; set; }

        /// <summary>
        /// The title of the question, e.g. First Reading
        /// </summary>
        public string QuestionSubtitle { get; set; }

        /// <summary>
        /// A description of the question, e.g. That the COVID-19 Public Health Response Amendment Bill be now read a first time
        /// </summary>
        public string QuestionDescription { get; set; }

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
    }
}
