using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParliamentVotes.Models.Legislation;
using ParliamentVotes.Models.Motions;
using ParliamentVotes.Models.Organisational;
using ParliamentVotes.Models.Votes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParliamentVotes.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Tenure>().HasKey(t => new { t.Member_Id, t.Start });
            builder.Entity<SplitPartyVote>().HasKey(t => new { t.PartyVote_Id, t.Member_Id });
            builder.Entity<GoverningParty>().HasKey(t => new { t.Parliament_Number, t.Party_Id });
            builder.Entity<Seating>().HasKey(t => new { t.Parliament_Number, t.SeatIndex, t.Member_Id });
            base.OnModelCreating(builder);
        }

        public DbSet<Party> Parties { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Tenure> Tenures { get; set; }
        public DbSet<Parliament> Parliaments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<PartyVote> PartyVotes { get; set; }
        public DbSet<PersonalVote> PersonalVotes { get; set; }
        public DbSet<VoiceVote> VoiceVotes { get; set; }
        public DbSet<SplitPartyVote> SplitPartyVotes { get; set; }
        public DbSet<Seating> SeatingPlans { get; set; }
        public DbSet<GoverningParty> GoverningParties { get; set; }
        
        public DbSet<Bill> Bills { get; set; }
        public DbSet<SupplementaryOrderPaper> SupplementaryOrderPapers { get; set; }
        public DbSet<Act> Acts { get; set; }
    }
}
