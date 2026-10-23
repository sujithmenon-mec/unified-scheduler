namespace ServiceScheduler.Api.Domain;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DealershipId { get; set; }
    public Guid ServiceTypeId { get; set; }
    public Guid ServiceBayId { get; set; }
    public Guid TechnicianId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public Customer Customer { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Dealership Dealership { get; set; } = null!;
    public ServiceType ServiceType { get; set; } = null!;
    public ServiceBay ServiceBay { get; set; } = null!;
    public Technician Technician { get; set; } = null!;
}