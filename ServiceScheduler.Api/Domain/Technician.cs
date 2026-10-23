namespace ServiceScheduler.Api.Domain;

public class Technician
{
    public Guid Id { get; set; }
    public Guid DealershipId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dealership Dealership { get; set; } = null!;
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}