using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> UtcConverter =
        new(value => value.ToUniversalTime(), value => value.ToUniversalTime());

    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Status).HasConversion<string>();
        builder.Property(appointment => appointment.StartUtc)
            .HasColumnType("timestamp with time zone")
            .HasConversion(UtcConverter);
        builder.Property(appointment => appointment.EndUtc)
            .HasColumnType("timestamp with time zone")
            .HasConversion(UtcConverter);
        builder.Property(appointment => appointment.CreatedUtc)
            .HasColumnType("timestamp with time zone")
            .HasConversion(UtcConverter);
        builder.HasIndex(appointment => new { appointment.ServiceBayId, appointment.StartUtc, appointment.EndUtc });
        builder.HasIndex(appointment => new { appointment.TechnicianId, appointment.StartUtc, appointment.EndUtc });
    }
}