using mcl959mvc.Classes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace mcl959mvc.Models;

public partial class Mcl959DbContext : DbContext
{

    public DbContextOptions<Mcl959DbContext> Options { get; }

    public Mcl959DbContext(DbContextOptions<Mcl959DbContext> options)
        : base(options)
    {
        Options = options;  
    }

    public virtual DbSet<CommentsModel> Comments { get; set; }

    public virtual DbSet<EventsModel> Events { get; set; }

    public virtual DbSet<MailgunLog> MailgunLogs { get; set; }

    public virtual DbSet<MemberRank> MemberRanks { get; set; }

    public virtual DbSet<MessagesModel> Messages { get; set; }

    public virtual DbSet<MemorialModel> Memorial { get; set; }

    public virtual DbSet<Roster> Roster { get; set; }

    public virtual DbSet<WebsiteLog> WebsiteLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessagesModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Message");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Roster>(entity =>
        {
            entity.Property(e => e.Authenticated).HasComment("True if Member has completed TwoStepAuthentication");
            entity.Property(e => e.DefaultInfo).HasComment("0=Not Set; 1=Default is Personal Info; 2=Default is Work Info;");
            entity.Property(e => e.DisplayName).HasComment("Use this if a member doesn't want to go by their full legal name.");
            entity.Property(e => e.HashPwd).HasComment("Encrypted Password");
            entity.Property(e => e.WebsiteDisplay).HasComment("0=Display Nothing on Website (Default); 1=Display Personal; 2=Display Work;");
        });

        modelBuilder.Entity<WebsiteLog>().ToTable("WebsiteLog");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
