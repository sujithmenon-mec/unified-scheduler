using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Domain;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Api.Application;

public class AvailabilityService(SchedulerDbContext dbContext)
{
    private static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(30);

    public async Task<AvailabilityResult> GetSlotsAsync(
        Guid dealershipId,
        Guid serviceTypeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (dealershipId == Guid.Empty || serviceTypeId == Guid.Empty)
        {
            return AvailabilityResult.Failure("VALIDATION_ERROR", ServiceErrorCategory.BadRequest);
        }

        var dealership = await dbContext.Dealerships
            .SingleOrDefaultAsync(item => item.Id == dealershipId, cancellationToken);
        if (dealership is null)
        {
            return AvailabilityResult.Failure("DEALERSHIP_NOT_FOUND", ServiceErrorCategory.NotFound);
        }

        var serviceType = await dbContext.ServiceTypes
            .SingleOrDefaultAsync(item => item.Id == serviceTypeId, cancellationToken);
        if (serviceType is null)
        {
            return AvailabilityResult.Failure("SERVICE_TYPE_NOT_FOUND", ServiceErrorCategory.NotFound);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(dealership.TimeZoneId);
        var slots = new List<AvailabilitySlot>();
        for (var localStart = dealership.OpenTime;
             localStart.Add(SlotInterval) <= dealership.CloseTime;
             localStart = localStart.Add(SlotInterval))
        {
            var localEnd = localStart.AddMinutes(serviceType.DurationMinutes);
            if (localEnd > dealership.CloseTime)
            {
                continue;
            }

            var startUtc = ToUtc(date, localStart, timeZone);
            var endUtc = startUtc.AddMinutes(serviceType.DurationMinutes);

            var freeBays = await dbContext.ServiceBays
                .CountAsync(bay => bay.DealershipId == dealership.Id && bay.IsActive &&
                    !dbContext.Appointments.Any(appointment =>
                        appointment.ServiceBayId == bay.Id &&
                        appointment.Status == AppointmentStatus.Confirmed &&
                        appointment.StartUtc < endUtc && appointment.EndUtc > startUtc), cancellationToken);
            var freeTechnicians = await dbContext.Technicians
                .CountAsync(technician => technician.DealershipId == dealership.Id &&
                    technician.Skills.Any(skill => skill.Id == serviceType.RequiredSkillId) &&
                    !dbContext.Appointments.Any(appointment =>
                        appointment.TechnicianId == technician.Id &&
                        appointment.Status == AppointmentStatus.Confirmed &&
                        appointment.StartUtc < endUtc && appointment.EndUtc > startUtc), cancellationToken);

            slots.Add(new AvailabilitySlot(startUtc, endUtc, freeBays > 0 && freeTechnicians > 0,
                freeBays, freeTechnicians));
        }

        return AvailabilityResult.Success(slots);
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo timeZone)
    {
        var localDateTime = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone), TimeSpan.Zero);
    }
}