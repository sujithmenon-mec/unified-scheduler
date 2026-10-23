using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.HasKey(serviceType => serviceType.Id);
        builder.HasMany(serviceType => serviceType.Appointments)
            .WithOne(appointment => appointment.ServiceType)
            .HasForeignKey(appointment => appointment.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}