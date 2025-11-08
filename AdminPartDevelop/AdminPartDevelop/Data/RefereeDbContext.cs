using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AdminPartDevelop.Models;

//NEW
public partial class RefereeDbContext : DbContext
{
    public RefereeDbContext()
    {
    }

    public RefereeDbContext(DbContextOptions<RefereeDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Excuse> Excuses { get; set; }

    public virtual DbSet<Referee> Referees { get; set; }

    public virtual DbSet<VehicleSlot> VehicleSlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Excuse>(entity =>
        {
            entity.HasKey(e => new { e.ExcuseId, e.RefereeId }).HasName("pk_excuses");

            entity.Property(e => e.ExcuseId)
                .ValueGeneratedOnAdd()
                .HasColumnName("excuse_id");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");
            entity.Property(e => e.DateFrom).HasColumnName("date_from");
            entity.Property(e => e.DateTo).HasColumnName("date_to");
            entity.Property(e => e.DatetimeAdded)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datetime_added");
            entity.Property(e => e.Note)
                .HasMaxLength(256)
                .HasColumnName("note");
            entity.Property(e => e.Reason)
                .HasMaxLength(256)
                .HasColumnName("reason");
            entity.Property(e => e.TimeFrom).HasColumnName("time_from");
            entity.Property(e => e.TimeTo).HasColumnName("time_to");

            entity.HasOne(d => d.Referee).WithMany(p => p.Excuses)
                .HasForeignKey(d => d.RefereeId)
                .HasConstraintName("fk_excuses_referees");
        });

        modelBuilder.Entity<Referee>(entity =>
        {
            entity.HasKey(e => e.RefereeId).HasName("pk_referees");

            entity.HasIndex(e => e.Email, "uc_referees_email").IsUnique();

            entity.Property(e => e.RefereeId).HasColumnName("referee_id");
            entity.Property(e => e.ActuallPragueZone)
                .HasMaxLength(100)
                .HasColumnName("actuall_prague_zone");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.CarAvailability).HasColumnName("car_availability");
            entity.Property(e => e.Email)
                .HasMaxLength(320)
                .HasColumnName("email");
            entity.Property(e => e.FacrId)
                .HasMaxLength(25)
                .HasColumnName("facr_id");
            entity.Property(e => e.Latitude).HasColumnName("latitude");
            entity.Property(e => e.League).HasColumnName("league");
            entity.Property(e => e.Longitude).HasColumnName("longitude");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.Ofs).HasColumnName("ofs");
            entity.Property(e => e.PragueZone)
                .HasMaxLength(100)
                .HasColumnName("prague_zone");
            entity.Property(e => e.Rating)
                .HasDefaultValue(7)
                .HasColumnName("rating");
            entity.Property(e => e.RefereeAddress)
                .HasMaxLength(200)
                .HasColumnName("referee_address");
            entity.Property(e => e.Surname)
                .HasMaxLength(100)
                .HasColumnName("surname");
            entity.Property(e => e.TimestampChange)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("timestamp_change");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<VehicleSlot>(entity =>
        {
            entity.HasKey(e => new { e.SlotId, e.RefereeId }).HasName("pk_vehicleslots");

            entity.Property(e => e.SlotId)
                .ValueGeneratedOnAdd()
                .HasColumnName("slot_id");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");
            entity.Property(e => e.DateFrom).HasColumnName("date_from");
            entity.Property(e => e.DateTo).HasColumnName("date_to");
            entity.Property(e => e.DatetimeAdded)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datetime_added");
            entity.Property(e => e.HasCarInTheSlot).HasColumnName("has_car_in_the_slot");
            entity.Property(e => e.TimeFrom).HasColumnName("time_from");
            entity.Property(e => e.TimeTo).HasColumnName("time_to");

            entity.HasOne(d => d.Referee).WithMany(p => p.VehicleSlots)
                .HasForeignKey(d => d.RefereeId)
                .HasConstraintName("fk_vehicleslots_referees");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}