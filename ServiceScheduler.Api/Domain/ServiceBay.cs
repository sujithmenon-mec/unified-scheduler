namespace ServiceScheduler.Api.Domain;

public class ServiceBay
{
    public Guid Id { get; set; }
    public Guid DealershipId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Dealership Dealership { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}