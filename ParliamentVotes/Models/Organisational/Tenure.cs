using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Models.Organisational
{
    public class Tenure
    {
        public Tenure() { }

        public void Update(Member member, DateTime begin, Party party, string electorate = null, DateTime? end = null)
        {
            Member = member;
            Start = begin;
            Party = party;
            Electorate = electorate;
            End = end;
        }

        /// <summary>
        /// The ID of the member this tenure represents
        /// </summary>
    [Required]
        public int Member_Id { get; set; }

        /// <summary>
        /// A virtual object that represents the member
        /// </summary>
        [ForeignKey("Member_Id")]
        public virtual Member Member { get; set; }

        /// <summary>
        /// The start of this tenure
        /// </summary>
        [Required]
        public DateTime Start { get; set; }

        /// <summary>
        /// The end of this tenure (if applicable)
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// The party this member belonged too (if applicable)
        /// </summary>
        [Required]
        public int Party_Id { get; set; }

        [ForeignKey("Party_Id")]
        public virtual Party Party { get; set; }

        /// <summary>
        /// The electorate the member represented during the tenure (if applicable)
        /// </summary>
        public string Electorate { get; set; }
    }
}
