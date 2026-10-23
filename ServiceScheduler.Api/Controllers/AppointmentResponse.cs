using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Controllers;

public sealed record AppointmentResponse(
    Guid Id,
    Guid CustomerId,
    Guid VehicleId,
    Guid DealershipId,
    Guid ServiceTypeId,
    Guid ServiceBayId,
    Guid TechnicianId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status)
{
    public static AppointmentResponse From(Appointment appointment) => new(
        appointment.Id,
        appointment.CustomerId,
        appointment.VehicleId,
        appointment.DealershipId,
        appointment.ServiceTypeId,
        appointment.ServiceBayId,
        appointment.TechnicianId,
        appointment.StartUtc,
        appointment.EndUtc,
        appointment.Status.ToString());
}