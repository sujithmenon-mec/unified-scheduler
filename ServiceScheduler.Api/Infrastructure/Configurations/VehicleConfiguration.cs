using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(vehicle => vehicle.Id);
        builder.HasIndex(vehicle => vehicle.Vin).IsUnique();
        builder.HasMany(vehicle => vehicle.Appointments)
            .WithOne(appointment => appointment.Vehicle)
            .HasForeignKey(appointment => appointment.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}