﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ParliamentVotes.Data;

namespace ParliamentVotes.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20210114115000_OptionalSopBill")]
    partial class OptionalSopBill
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(256)")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Legislation.Act", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ActType")
                        .HasColumnType("int");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Acts");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Legislation.Bill", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("BillNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DirectoryUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<int?>("Member_Id")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Member_Id");

                    b.ToTable("Bills");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Legislation.SupplementaryOrderPaper", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("AmendingBill_Id")
                        .HasColumnType("int");

                    b.Property<string>("DirectoryUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("datetime2");

                    b.Property<int>("Member_Id")
                        .HasColumnType("int");

                    b.Property<int>("Number")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<int>("Year")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AmendingBill_Id");

                    b.HasIndex("Member_Id");

                    b.ToTable("SupplementaryOrderPapers");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Motions.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("Bill_Id")
                        .HasColumnType("int");

                    b.Property<string>("Clause")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("Member_Id")
                        .HasColumnType("int");

                    b.Property<int>("Parliament_Number")
                        .HasColumnType("int");

                    b.Property<bool?>("PersonalVoteConservativeViewPoint")
                        .HasColumnType("bit");

                    b.Property<int>("QuestionType")
                        .HasColumnType("int");

                    b.Property<int?>("Stage")
                        .HasColumnType("int");

                    b.Property<string>("Subtitle")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("SupplementaryOrderPaper_Id")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Bill_Id");

                    b.HasIndex("Member_Id");

                    b.HasIndex("Parliament_Number");

                    b.HasIndex("SupplementaryOrderPaper_Id");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.GoverningParty", b =>
                {
                    b.Property<int>("Parliament_Number")
                        .HasColumnType("int");

                    b.Property<int>("Party_Id")
                        .HasColumnType("int");

                    b.Property<int?>("Relationship")
                        .HasColumnType("int");

                    b.HasKey("Parliament_Number", "Party_Id");

                    b.HasIndex("Party_Id");

                    b.ToTable("GoverningParties");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Member", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlsoKnownAs")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageCopyright")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ImageUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Members");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Parliament", b =>
                {
                    b.Property<int>("Number")
                        .HasColumnType("int");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Number");

                    b.ToTable("Parliaments");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Party", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AlsoKnownAs")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Color")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LogoUrl")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Parties");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Seating", b =>
                {
                    b.Property<int>("Parliament_Number")
                        .HasColumnType("int");

                    b.Property<int>("SeatIndex")
                        .HasColumnType("int");

                    b.Property<int>("Member_Id")
                        .HasColumnType("int");

                    b.HasKey("Parliament_Number", "SeatIndex", "Member_Id");

                    b.HasIndex("Member_Id");

                    b.ToTable("SeatingPlans");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Tenure", b =>
                {
                    b.Property<int>("Member_Id")
                        .HasColumnType("int");

                    b.Property<DateTime>("Start")
                        .HasColumnType("datetime2");

                    b.Property<string>("Electorate")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("End")
                        .HasColumnType("datetime2");

                    b.Property<int>("Party_Id")
                        .HasColumnType("int");

                    b.HasKey("Member_Id", "Start");

                    b.HasIndex("Party_Id");

                    b.ToTable("Tenures");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.PartyVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ComplexPosition")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("Member_Id")
                        .HasColumnType("int");

                    b.Property<int>("NumberOfVotes")
                        .HasColumnType("int");

                    b.Property<int?>("Party_Id")
                        .HasColumnType("int");

                    b.Property<bool?>("Position")
                        .HasColumnType("bit");

                    b.Property<int>("Question_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Member_Id");

                    b.HasIndex("Party_Id");

                    b.HasIndex("Question_Id");

                    b.ToTable("PartyVotes");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.PersonalVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ComplexPosition")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Member_Id")
                        .HasColumnType("int");

                    b.Property<bool?>("Position")
                        .HasColumnType("bit");

                    b.Property<bool>("Proxy")
                        .HasColumnType("bit");

                    b.Property<int>("Question_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Member_Id");

                    b.HasIndex("Question_Id");

                    b.ToTable("PersonalVotes");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.SplitPartyVote", b =>
                {
                    b.Property<int>("PartyVote_Id")
                        .HasColumnType("int");

                    b.Property<int>("Member_Id")
                        .HasColumnType("int");

                    b.HasKey("PartyVote_Id", "Member_Id");

                    b.HasIndex("Member_Id");

                    b.ToTable("SplitPartyVotes");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.VoiceVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ComplexPosition")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("Position")
                        .HasColumnType("bit");

                    b.Property<int>("Question_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Question_Id");

                    b.ToTable("VoiceVotes");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Legislation.Bill", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Legislation.SupplementaryOrderPaper", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Legislation.Bill", "AmendingBill")
                        .WithMany()
                        .HasForeignKey("AmendingBill_Id");

                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Motions.Question", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Legislation.Bill", "Bill")
                        .WithMany()
                        .HasForeignKey("Bill_Id");

                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id");

                    b.HasOne("ParliamentVotes.Models.Organisational.Parliament", "Parliament")
                        .WithMany()
                        .HasForeignKey("Parliament_Number")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Legislation.SupplementaryOrderPaper", "SupplementaryOrderPaper")
                        .WithMany()
                        .HasForeignKey("SupplementaryOrderPaper_Id");
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.GoverningParty", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Parliament", "Parliament")
                        .WithMany("GoverningParties")
                        .HasForeignKey("Parliament_Number")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Organisational.Party", "Party")
                        .WithMany()
                        .HasForeignKey("Party_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Seating", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Organisational.Parliament", "Parliament")
                        .WithMany("SeatingPlan")
                        .HasForeignKey("Parliament_Number")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Organisational.Tenure", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany("Tenures")
                        .HasForeignKey("Member_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Organisational.Party", "Party")
                        .WithMany()
                        .HasForeignKey("Party_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.PartyVote", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id");

                    b.HasOne("ParliamentVotes.Models.Organisational.Party", "Party")
                        .WithMany()
                        .HasForeignKey("Party_Id");

                    b.HasOne("ParliamentVotes.Models.Motions.Question", "Question")
                        .WithMany()
                        .HasForeignKey("Question_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.PersonalVote", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Motions.Question", "Question")
                        .WithMany()
                        .HasForeignKey("Question_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.SplitPartyVote", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Organisational.Member", "Member")
                        .WithMany()
                        .HasForeignKey("Member_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ParliamentVotes.Models.Votes.PartyVote", "PartyVote")
                        .WithMany("SplitPartyVotes")
                        .HasForeignKey("PartyVote_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ParliamentVotes.Models.Votes.VoiceVote", b =>
                {
                    b.HasOne("ParliamentVotes.Models.Motions.Question", "Question")
                        .WithMany()
                        .HasForeignKey("Question_Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
