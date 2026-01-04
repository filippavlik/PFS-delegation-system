using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AdminPartDevelop.Models;

public partial class AdminDbContext : DbContext
{
    public AdminDbContext()
    {
    }

    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<Field> Fields { get; set; }

    public virtual DbSet<FilesPreviousDelegation> FilesPreviousDelegations { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<StartingGameDate> StartingGameDates { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Transfer> Transfers { get; set; }

    public virtual DbSet<Veto> Vetoes { get; set; }
    public virtual DbSet<CustomCompetitionRules> CustomCompetitionRules { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.CompetitionId).HasName("pk_competitions");

            entity.Property(e => e.CompetitionId)
                .HasMaxLength(15)
                .HasColumnName("competition_id");
            entity.Property(e => e.CompetitionName)
                .HasMaxLength(100)
                .HasColumnName("competition_name");
            entity.Property(e => e.League).HasColumnName("league");
            entity.Property(e => e.AmountOfReferees).HasColumnName("amount_of_referees");
            entity.Property(e => e.MatchLength).HasColumnName("match_length");
        });

        modelBuilder.Entity<Field>(entity =>
        {
            entity.HasKey(e => e.FieldId).HasName("pk_fields");

            entity.Property(e => e.FieldId).HasColumnName("field_id");
            entity.Property(e => e.FieldAddress)
                .HasMaxLength(200)
                .HasColumnName("field_address");
            entity.Property(e => e.FieldName)
                .HasMaxLength(100)
                .HasColumnName("field_name");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
        });

        modelBuilder.Entity<FilesPreviousDelegation>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("pk_files_previous_delegations");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.AmountOfMatches).HasColumnName("amount_of_matches");
            entity.Property(e => e.DelegationsFrom).HasColumnName("delegations_from");
            entity.Property(e => e.DelegationsTo).HasColumnName("delegations_to");
            entity.Property(e => e.FileName)
                .HasMaxLength(80)
                .HasColumnName("file_name");
            entity.Property(e => e.FileUploadedBy).HasMaxLength(320).HasColumnName("file_uploaded_by");
            entity.Property(e => e.FileUploadedDatetime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("file_uploaded_datetime");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => new { e.MatchId, e.CompetitionId, e.FieldId }).HasName("pk_matches");

            entity.Property(e => e.MatchId)
                .HasMaxLength(20)
                .HasColumnName("match_id");
            entity.Property(e => e.CompetitionId)
                .HasMaxLength(15)
                .HasColumnName("competition_id");
            entity.Property(e => e.FieldId).HasColumnName("field_id");
            entity.Property(e => e.AlreadyPlayed).HasColumnName("already_played");
            entity.Property(e => e.Ar1Id).HasColumnName("ar1_id");
            entity.Property(e => e.Ar2Id).HasColumnName("ar2_id");
            entity.Property(e => e.AwayTeamId)
                .HasMaxLength(30)
                .HasColumnName("away_team_id");
            entity.Property(e => e.HomeTeamId)
                .HasMaxLength(30)
                .HasColumnName("home_team_id");
            entity.Property(e => e.LastChanged)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_changed");
            entity.Property(e => e.LastChangedBy)
            .HasMaxLength(320)
            .HasColumnName("last_changed_by");
            entity.Property(e => e.Locked).HasColumnName("locked");
            entity.Property(e => e.MatchDate).HasColumnName("match_date");
            entity.Property(e => e.MatchTime).HasColumnName("match_time");
            entity.Property(e => e.Note)
                .HasMaxLength(400)
                .HasColumnName("note");
            entity.Property(e => e.PostMatch)
                .HasMaxLength(20)
                .HasColumnName("post_match");
            entity.Property(e => e.PreMatch)
                .HasMaxLength(20)
                .HasColumnName("pre_match");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");

            entity.HasOne(d => d.Competition).WithMany(p => p.Matches)
                .HasForeignKey(d => d.CompetitionId)
                .HasConstraintName("fk_matches_competitions");

            entity.HasOne(d => d.Field).WithMany(p => p.Matches)
                .HasForeignKey(d => d.FieldId)
                .HasConstraintName("fk_matches_fields");
        });

        modelBuilder.Entity<StartingGameDate>(entity =>
        {
            entity.HasKey(e => e.GameDateId).HasName("StartingGameDates_pkey");

            entity.Property(e => e.GameDateId)
                .ValueGeneratedNever()
                .HasColumnName("game_date_id");
            entity.Property(e => e.GameDate).HasColumnName("game_date");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("pk_teams");

            entity.Property(e => e.TeamId)
                .HasMaxLength(30)
                .HasColumnName("team_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");

            entity.HasMany(d => d.Matches).WithMany(p => p.Teams)
                .UsingEntity<Dictionary<string, object>>(
                    "TeamsMatch",
                    r => r.HasOne<Match>().WithMany()
                        .HasForeignKey("MatchId", "CompetitionId", "FieldId")
                        .HasConstraintName("fk_teams_matches_matches"),
                    l => l.HasOne<Team>().WithMany()
                        .HasForeignKey("TeamId")
                        .HasConstraintName("fk_teams_matches_teams"),
                    j =>
                    {
                        j.HasKey("TeamId", "MatchId", "CompetitionId", "FieldId").HasName("pk_teams_matches");
                        j.ToTable("TeamsMatches");
                        j.IndexerProperty<string>("TeamId")
                            .HasMaxLength(30)
                            .HasColumnName("team_id");
                        j.IndexerProperty<string>("MatchId")
                            .HasMaxLength(20)
                            .HasColumnName("match_id");
                        j.IndexerProperty<string>("CompetitionId")
                            .HasMaxLength(15)
                            .HasColumnName("competition_id");
                        j.IndexerProperty<int>("FieldId").HasColumnName("field_id");
                    });
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.HasKey(e => e.TransferId).HasName("pk_transfers");

            entity.Property(e => e.TransferId).HasColumnName("transfer_id");
            entity.Property(e => e.Car).HasColumnName("car");
            entity.Property(e => e.ExpectedArrival)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expected_arrival");
            entity.Property(e => e.ExpectedDeparture)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expected_departure");
            entity.Property(e => e.FromHome).HasColumnName("from_home");
            entity.Property(e => e.FutureMatchId)
                .HasMaxLength(20)
                .HasColumnName("future_match_id");
            entity.Property(e => e.PreviousMatchId)
                .HasMaxLength(20)
                .HasColumnName("previous_match_id");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");
        });

        modelBuilder.Entity<Veto>(entity =>
        {
            entity.HasKey(e => new { e.VetoId, e.CompetitionId, e.TeamId }).HasName("pk_vetoes");

            entity.Property(e => e.VetoId)
                .ValueGeneratedOnAdd()
                .HasColumnName("veto_id");
            entity.Property(e => e.CompetitionId)
                .HasMaxLength(15)
                .HasColumnName("competition_id");
            entity.Property(e => e.TeamId)
                .HasMaxLength(30)
                .HasColumnName("team_id");
            entity.Property(e => e.Note)
                .HasMaxLength(256)
                .HasColumnName("note");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");

            entity.HasOne(d => d.Competition).WithMany(p => p.Vetoes)
                .HasForeignKey(d => d.CompetitionId)
                .HasConstraintName("fk_vetoes_competitions");

            entity.HasOne(d => d.Team).WithMany(p => p.Vetoes)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("fk_vetoes_teams");
        });
        modelBuilder.Entity<CustomCompetitionRules>(entity =>
        {
            entity.HasKey(e => e.CustomCompetitionRuleId)
                  .HasName("pk_custom_competition_rules");

            entity.Property(e => e.CustomCompetitionRuleId)
                  .HasColumnName("custom_competition_rule_id");

            entity.Property(e => e.RefereeId)
                  .HasColumnName("referee_id");

            entity.Property(e => e.CompetitionId)
                  .HasColumnName("competition_id");

            entity.Property(e => e.isAdded)
                  .HasColumnName("is_added");

            // FK → Competition
            entity.HasOne(d => d.Competition)
                  .WithMany(p => p.CustomCompetitionRules)
                  .HasForeignKey(d => d.CompetitionId)
                  .HasConstraintName("fk_custom_rules_competition");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
