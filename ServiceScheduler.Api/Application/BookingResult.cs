using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Application;

public enum ServiceErrorCategory
{
    BadRequest,
    NotFound,
    Conflict
}

public sealed record ServiceError(string Code, ServiceErrorCategory Category);

public sealed record BookingResult(Appointment? Appointment, ServiceError? Error)
{
    public bool Succeeded => Appointment is not null;

    public static BookingResult Success(Appointment appointment) => new(appointment, null);
    public static BookingResult Failure(string code, ServiceErrorCategory category) => new(null, new(code, category));
}

public sealed record BookingRequest(Guid VehicleId, Guid DealershipId, Guid ServiceTypeId, string? StartTime);