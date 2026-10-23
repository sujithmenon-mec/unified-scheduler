using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Infrastructure.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.HasKey(skill => skill.Id);
        builder.HasIndex(skill => skill.Name).IsUnique();
        builder.HasMany(skill => skill.ServiceTypes)
            .WithOne(serviceType => serviceType.RequiredSkill)
            .HasForeignKey(serviceType => serviceType.RequiredSkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}