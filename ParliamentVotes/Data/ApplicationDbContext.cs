using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
            builder.Entity<GoverningParty>().HasKey(t => new { t.Session_Number, t.Party_Id });
            base.OnModelCreating(builder);
        }

        public DbSet<Party> Parties { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Tenure> Tenures { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<PartyVote> PartyVotes { get; set; }
        public DbSet<PersonalVote> PersonalVotes { get; set; }
        public DbSet<VoiceVote> VoiceVotes { get; set; }
        public DbSet<SplitPartyVote> SplitPartyVotes { get; set; }
    }
}
