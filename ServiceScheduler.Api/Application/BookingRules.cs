using ServiceScheduler.Api.Domain;

namespace ServiceScheduler.Api.Application;

public static class BookingRules
{
    public static bool IsWithinWorkingHours(
        TimeWindow window,
        TimeZoneInfo timeZone,
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        var localStart = TimeZoneInfo.ConvertTime(window.Start, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(window.End, timeZone);
        return localStart.Date == localEnd.Date &&
               TimeOnly.FromDateTime(localStart.DateTime) >= openTime &&
               TimeOnly.FromDateTime(localEnd.DateTime) <= closeTime;
    }

    public static bool HasStarted(TimeWindow window, TimeProvider timeProvider) =>
        window.Start <= timeProvider.GetUtcNow();
}
