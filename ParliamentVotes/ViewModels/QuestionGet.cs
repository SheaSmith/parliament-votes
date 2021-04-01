using ParliamentVotes.Models.Motions;
using ParliamentVotes.Models.Votes;
using ParliamentVotes.ViewModels.Legislation;
using ParliamentVotes.ViewModels.Organisational;
using ParliamentVotes.ViewModels.Votes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.ViewModels
{
    public class QuestionGet
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string Description { get; set; }

        public DateTime Timestamp { get; set; }

        public QuestionType Type { get; set; }

        public Stage? Stage { get; set; }

        public string Clause { get; set; }

        public bool? PersonalVoteConservativeViewPoint { get; set; }

        public MemberGet Member { get; set; }

        public BillGet Bill { get; set; }

        public SupplementaryOrderPaperGet SupplementaryOrderPaper { get; set; }

        public ParliamentGet Parliament { get; set; }

        public VoiceVoteGet VoiceVote { get; set; }

        public IEnumerable<PartyVoteGet> PartyVotes { get; set; }

        public IEnumerable<PersonalVoteGet> PersonalVotes { get; set; }


        public QuestionGet() { }

        public QuestionGet(Question question, bool includeBill = true)
        {
            Id = question.Id;
            Title = question.Title;
            Subtitle = question.Subtitle;
            Description = question.Description;
            Timestamp = question.Timestamp;
            Type = question.QuestionType;
            Stage = question.Stage;
            Clause = question.Clause;
            PersonalVoteConservativeViewPoint = question.PersonalVoteConservativeViewPoint;
            Member = question.Member == null ? null : new MemberGet(question.Member);
            if (includeBill)
                Bill = question.Bill == null ? null : new BillGet(question.Bill, false);
            SupplementaryOrderPaper = question.SupplementaryOrderPaper == null ? null : new SupplementaryOrderPaperGet(question.SupplementaryOrderPaper);
            Parliament = question.Parliament == null ? null : new ParliamentGet(question.Parliament);
            VoiceVote = question.VoiceVotes == null || question.VoiceVotes.Count() == 0 ? null : new VoiceVoteGet(question.VoiceVotes.FirstOrDefault());
            PartyVotes = question.PartyVotes == null || question.PartyVotes.Count() == 0 ? null : question.PartyVotes.Select(p => new PartyVoteGet(p));
            PersonalVotes = question.PersonalVotes == null || question.PersonalVotes.Count() == 0 ? null : question.PersonalVotes.Select(p => new PersonalVoteGet(p));
        }
    }
}
