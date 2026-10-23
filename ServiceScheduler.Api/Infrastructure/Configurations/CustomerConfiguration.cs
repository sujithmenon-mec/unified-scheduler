using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.HasMany(customer => customer.Vehicles)
            .WithOne(vehicle => vehicle.Customer)
            .HasForeignKey(vehicle => vehicle.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(customer => customer.Appointments)
            .WithOne(appointment => appointment.Customer)
            .HasForeignKey(appointment => appointment.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}