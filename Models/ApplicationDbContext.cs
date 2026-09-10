using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bill> Bills { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<CheckIn> CheckIns { get; set; }

    public virtual DbSet<CheckOut> CheckOuts { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bills__3214EC078A7D46AF");

            entity.HasIndex(e => e.BookingId, "UQ__Bills__73951AECCA09EF5F").IsUnique();

            entity.Property(e => e.BillDate).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Booking).WithOne(p => p.Bill)
                .HasForeignKey<Bill>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bills__BookingId__656C112C");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bookings__3214EC075FE96888");

            entity.Property(e => e.BookingDate).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bookings__Custom__5535A963");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Bookings__RoomId__5629CD9C");
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CheckIns__3214EC07232D5326");

            entity.HasIndex(e => e.BookingId, "UQ__CheckIns__73951AECBEE6372F").IsUnique();

            entity.Property(e => e.CheckInDateTime).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Booking).WithOne(p => p.CheckIn)
                .HasForeignKey<CheckIn>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckIns__Bookin__5BE2A6F2");
        });

        modelBuilder.Entity<CheckOut>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CheckOut__3214EC072B873976");

            entity.HasIndex(e => e.CheckInId, "UQ__CheckOut__E6497685C37AF93D").IsUnique();

            entity.Property(e => e.CheckOutDateTime).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.CheckIn).WithOne(p => p.CheckOut)
                .HasForeignKey<CheckOut>(d => d.CheckInId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckOuts__Check__60A75C0F");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC078EC09E24");

            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC072C501168");

            entity.HasIndex(e => e.RoomNumber, "UQ__Rooms__AE10E07A3BC7807B").IsUnique();

            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.PricePerNight).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.RoomNumber).HasMaxLength(20);

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Rooms__RoomTypeI__4E88ABD4");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoomType__3214EC078B4222AF");

            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
