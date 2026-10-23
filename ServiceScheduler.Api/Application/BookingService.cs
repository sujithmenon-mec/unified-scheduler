using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ServiceScheduler.Api.Domain;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Api.Application;

public class BookingService(SchedulerDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<BookingResult> BookAsync(BookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.VehicleId == Guid.Empty || request.DealershipId == Guid.Empty ||
            request.ServiceTypeId == Guid.Empty || !TryParseOffset(request.StartTime, out var requestedStart))
        {
            return BookingResult.Failure("VALIDATION_ERROR", ServiceErrorCategory.BadRequest);
        }

        var vehicle = await dbContext.Vehicles
            .Include(item => item.Customer)
            .SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return BookingResult.Failure("VEHICLE_NOT_FOUND", ServiceErrorCategory.NotFound);
        }

        var dealership = await dbContext.Dealerships
            .SingleOrDefaultAsync(item => item.Id == request.DealershipId, cancellationToken);
        if (dealership is null)
        {
            return BookingResult.Failure("DEALERSHIP_NOT_FOUND", ServiceErrorCategory.NotFound);
        }

        var serviceType = await dbContext.ServiceTypes
            .Include(item => item.RequiredSkill)
            .SingleOrDefaultAsync(item => item.Id == request.ServiceTypeId, cancellationToken);
        if (serviceType is null)
        {
            return BookingResult.Failure("SERVICE_TYPE_NOT_FOUND", ServiceErrorCategory.NotFound);
        }

        var window = new TimeWindow(requestedStart, requestedStart.AddMinutes(serviceType.DurationMinutes));
        var now = timeProvider.GetUtcNow();
        if (window.Start <= now)
        {
            return BookingResult.Failure("START_IN_PAST", ServiceErrorCategory.BadRequest);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(dealership.TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(window.Start, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(window.End, timeZone);
        if (localStart.Date != localEnd.Date || TimeOnly.FromDateTime(localStart.DateTime) < dealership.OpenTime ||
            TimeOnly.FromDateTime(localEnd.DateTime) > dealership.CloseTime)
        {
            return BookingResult.Failure("OUTSIDE_WORKING_HOURS", ServiceErrorCategory.BadRequest);
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                var freeBay = await dbContext.ServiceBays
                    .Where(bay => bay.DealershipId == dealership.Id && bay.IsActive)
                    .Where(bay => !dbContext.Appointments.Any(appointment =>
                        appointment.ServiceBayId == bay.Id &&
                        appointment.Status == AppointmentStatus.Confirmed &&
                        appointment.StartUtc < window.End && appointment.EndUtc > window.Start))
                    .OrderBy(bay => bay.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                if (freeBay is null)
                {
                    return BookingResult.Failure("NO_BAY_AVAILABLE", ServiceErrorCategory.Conflict);
                }

                var freeTechnician = await dbContext.Technicians
                    .Where(technician => technician.DealershipId == dealership.Id)
                    .Where(technician => technician.Skills.Any(skill => skill.Id == serviceType.RequiredSkillId))
                    .Where(technician => !dbContext.Appointments.Any(appointment =>
                        appointment.TechnicianId == technician.Id &&
                        appointment.Status == AppointmentStatus.Confirmed &&
                        appointment.StartUtc < window.End && appointment.EndUtc > window.Start))
                    .OrderBy(technician => technician.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                if (freeTechnician is null)
                {
                    return BookingResult.Failure("NO_TECHNICIAN_AVAILABLE", ServiceErrorCategory.Conflict);
                }

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    CustomerId = vehicle.CustomerId,
                    VehicleId = vehicle.Id,
                    DealershipId = dealership.Id,
                    ServiceTypeId = serviceType.Id,
                    ServiceBayId = freeBay.Id,
                    TechnicianId = freeTechnician.Id,
                    StartUtc = window.Start,
                    EndUtc = window.End,
                    Status = AppointmentStatus.Confirmed,
                    CreatedUtc = now
                };

                dbContext.Appointments.Add(appointment);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return BookingResult.Success(appointment);
            }
            catch (Exception exception) when (IsSerializationFailure(exception))
            {
                dbContext.ChangeTracker.Clear();
                if (attempt == 2)
                {
                    return BookingResult.Failure("CONCURRENT_BOOKING_CONFLICT", ServiceErrorCategory.Conflict);
                }
            }
        }

        return BookingResult.Failure("CONCURRENT_BOOKING_CONFLICT", ServiceErrorCategory.Conflict);
    }

    private static bool TryParseOffset(string? value, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var timeSeparator = value.IndexOf('T');
        var hasExplicitOffset = value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
                                (timeSeparator >= 0 && value.IndexOfAny(['+', '-'], timeSeparator + 1) >= 0);
        return hasExplicitOffset && DateTimeOffset.TryParse(
            value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
            {
                return true;
            }
        }

        return false;
    }
}