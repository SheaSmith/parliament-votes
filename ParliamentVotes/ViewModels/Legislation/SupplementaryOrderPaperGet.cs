using ParliamentVotes.Models.Legislation;
using ParliamentVotes.ViewModels.Organisational;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels.Legislation
{
    public class SupplementaryOrderPaperGet
    {
        public int Id { get; set; }

        public int Number { get; set; }

        public DateTime LastUpdated { get; set; }

        public DateTime Date { get; set; }

        public ParliamentGet Parliament { get; set; }

        public MemberGet Member { get; set; }

        public BillGet AmendingBill { get; set; }

        public SupplementaryOrderPaperType Type { get; set; }

        public SupplementaryOrderPaperGet() { }

        public SupplementaryOrderPaperGet(SupplementaryOrderPaper sop)
        {
            Id = sop.Id;
            Number = sop.Number;
            LastUpdated = sop.LastUpdated;
            Date = sop.Date;
            Parliament = new ParliamentGet(sop.Parliament);
            Member = new MemberGet(sop.Member);
            AmendingBill = sop.AmendingBill == null ? null : new BillGet(sop.AmendingBill, false);
            Type = sop.Type;
        }
    }
}
