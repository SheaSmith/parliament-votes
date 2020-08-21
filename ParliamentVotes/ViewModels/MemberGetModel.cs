using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class MemberGetModel
    {        
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public IEnumerable<TenureGetModel> Tenures { get; set; }

        public MemberGetModel(Member member)
        {
            Id = member.Id;
            Name = member.FirstName + " " + member.LastName;
            ImageUrl = member.ImageUrl;
            Tenures = member.Tenures.Select(t => new TenureGetModel(t));
        }
    }
}
