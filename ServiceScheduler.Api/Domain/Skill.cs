namespace ServiceScheduler.Api.Domain;

public class Skill
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Technician> Technicians { get; set; } = new List<Technician>();
    public ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
}