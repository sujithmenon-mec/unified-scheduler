using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class ServiceBayConfiguration : IEntityTypeConfiguration<ServiceBay>
{
    public void Configure(EntityTypeBuilder<ServiceBay> builder)
    {
        builder.HasKey(bay => bay.Id);
        builder.HasMany(bay => bay.Appointments)
            .WithOne(appointment => appointment.ServiceBay)
            .HasForeignKey(appointment => appointment.ServiceBayId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}