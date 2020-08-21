using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class Party
    {
        public Party() { }

        public Party(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// An auto generated ID for this party
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The name of this party
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// An HTML color that represents this party 
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// A logo URL to use
        /// </summary>
        public string LogoUrl { get; set; }

        /// <summary>
        /// If the party is known as something else for the purposes of party votes (e.g. the National Party is 'New Zealand National' for party vote purposes)
        /// </summary>
        public string AlsoKnownAs { get; set; }
    }
}
