using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.HasKey(dealership => dealership.Id);
        builder.HasMany(dealership => dealership.ServiceBays)
            .WithOne(bay => bay.Dealership)
            .HasForeignKey(bay => bay.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(dealership => dealership.Technicians)
            .WithOne(technician => technician.Dealership)
            .HasForeignKey(technician => technician.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(dealership => dealership.Appointments)
            .WithOne(appointment => appointment.Dealership)
            .HasForeignKey(appointment => appointment.DealershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}