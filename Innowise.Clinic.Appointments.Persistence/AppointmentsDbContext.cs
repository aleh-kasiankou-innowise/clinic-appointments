using Innowise.Clinic.Appointments.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Innowise.Clinic.Appointments.Persistence;

public class AppointmentsDbContext : DbContext
{
    public AppointmentsDbContext(DbContextOptions<AppointmentsDbContext> options) : base(options)
    {
    }

    public DbSet<Appointment> Appointments { get; init; }
    public DbSet<ReservedTimeSlot> ReservedTimeSlots { get; init; }
    public DbSet<AppointmentResult> AppointmentResults { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservedTimeSlot>()
            .HasKey(x => x.ReservedTimeSlotId);

        modelBuilder.Entity<AppointmentResult>()
            .HasKey(x => x.AppointmentResultId);

        modelBuilder.Entity<Appointment>()
            .HasKey(x => x.AppointmentId);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.ReservedTimeSlot)
            .WithOne(t => t.Appointment)
            .HasForeignKey<Appointment>(app => app.ReservedTimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.AppointmentResult)
            .WithOne(ar => ar.Appointment)
            .HasForeignKey<Appointment>(a => a.AppointmentResultId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}