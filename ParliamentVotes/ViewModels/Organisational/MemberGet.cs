using ParliamentVotes.Models.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Organisational
{
    public class MemberGet
    {        
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string ImageCopyright { get; set; }
        public IEnumerable<TenureGet> Tenures { get; set; }

        public MemberGet(Member member)
        {
            Id = member.Id;
            Name = member.FirstName + " " + member.LastName;
            ImageUrl = member.ImageUrl;
            Tenures = member.Tenures.Select(t => new TenureGet(t));
            ImageCopyright = member.ImageCopyright;
        }
    }
}
