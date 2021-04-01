using ParliamentVotes.Models.Legislation;
using ParliamentVotes.ViewModels.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Legislation
{
    public class BillGet
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Number { get; set; }

        public DateTime LastUpdated { get; set; }

        public IEnumerable<MemberGet> Members { get; set; }

        public BillType Type { get; set; }

        public IEnumerable<ParliamentGet> Parliaments { get; set; }

        public IEnumerable<QuestionGet> Questions { get; set; }

        public BillGet() { }

        public BillGet(Bill bill, bool includeAllVotes = true)
        {
            Id = bill.Id;
            Title = bill.Title;
            Description = bill.Description;
            Number = bill.BillNumber;
            LastUpdated = bill.LastUpdated;
            Members = bill.Members == null ? null : bill.Members.Select(m => new MemberGet(m));
            Type = bill.Type;
            Parliaments = bill.Parliaments.Select(p => new ParliamentGet(p));
            Questions = bill.Questions == null || bill.Questions.Count() == 0 ? null : bill.Questions.Where(q => includeAllVotes || q.QuestionType == Models.Motions.QuestionType.BillReading).Select(q => new QuestionGet(q, false));
        }
    }
}
