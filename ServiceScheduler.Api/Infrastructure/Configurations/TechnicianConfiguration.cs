using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.HasKey(technician => technician.Id);
        builder.HasMany(technician => technician.Appointments)
            .WithOne(appointment => appointment.Technician)
            .HasForeignKey(appointment => appointment.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(technician => technician.Skills)
            .WithMany(skill => skill.Technicians)
            .UsingEntity<Dictionary<string, object>>(
                "TechnicianSkill",
                right => right.HasOne<Skill>()
                    .WithMany()
                    .HasForeignKey("SkillId")
                    .OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Technician>()
                    .WithMany()
                    .HasForeignKey("TechnicianId")
                    .OnDelete(DeleteBehavior.Restrict),
                join => join.HasKey("TechnicianId", "SkillId"));
    }
}