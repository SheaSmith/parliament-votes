using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Legislation
{
    public class Act
    {
        /// <summary>
        /// The ID of the act
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The title of the act, e.g. Education Vocational Education and Training Reform Amendment Act 2020
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// When this act was last updated in our database
        /// </summary>
        [Required]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The type of this act
        /// </summary>
        [Required]
        public ActType ActType { get; set; }

        /// <summary>
        /// A slug for this version, which will lead us to the filename
        /// </summary>
        [Required]
        public string Slug { get; set; }

        /// <summary>
        /// The name of the XML file for this act
        /// </summary>
        [Required]
        public string FileName { get; set; }
    }

    public enum ActType
    {
        Imperial,
        Local,
        Private,
        Provincial,
        Public
    }
}
