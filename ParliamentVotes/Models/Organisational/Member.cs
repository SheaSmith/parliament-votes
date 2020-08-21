using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class Member
    {
        public Member() { }
        public Member(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        /// <summary>
        /// The ID of this member, autoincremented
        /// </summary>
        [Key, Required]
        public int Id { get; set; }

        /// <summary>
        /// The first name of this MP, not including any honorifics
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// The first name of this MP, not including any honorifics
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// An image of this member
        /// </summary>
        [Required]
        public string ImageUrl { get; set; }

        /// <summary>
        /// When this member has sat in parliament, what parties they represented, and what electorate(s) they represented
        /// </summary>
        public virtual List<Tenure> Tenures { get; set; }
    }
}
