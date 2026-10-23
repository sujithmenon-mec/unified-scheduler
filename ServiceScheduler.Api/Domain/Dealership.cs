namespace ServiceScheduler.Api.Domain;

public class Dealership
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public ICollection<ServiceBay> ServiceBays { get; set; } = new List<ServiceBay>();
    public ICollection<Technician> Technicians { get; set; } = new List<Technician>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}