namespace ServiceScheduler.Api.Domain;

public class ServiceType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public Guid RequiredSkillId { get; set; }
    public Skill RequiredSkill { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}