using System.Diagnostics.Metrics;
using ServiceScheduler.Api.Application;
using ServiceScheduler.Api.Domain;
using Microsoft.AspNetCore.Http;
using ServiceScheduler.Api.Infrastructure;

namespace ServiceScheduler.Tests;

public class UnitTest1
{
    public static IEnumerable<object[]> OverlapCases =>
    [
        ["identical", "10:00", "11:00", "10:00", "11:00", true],
        ["partial overlap at start", "10:00", "11:00", "10:30", "11:30", true],
        ["partial overlap at end", "10:00", "11:00", "09:30", "10:30", true],
        ["contained", "10:00", "12:00", "10:30", "11:30", true],
        ["containing", "10:30", "11:30", "10:00", "12:00", true],
        ["back-to-back before", "10:00", "11:00", "09:00", "10:00", false],
        ["back-to-back after", "10:00", "11:00", "11:00", "12:00", false],
        ["fully disjoint", "10:00", "11:00", "12:00", "13:00", false]
    ];

    [Theory]
    [MemberData(nameof(OverlapCases))]
    public void Overlaps_UsesHalfOpenBoundaries(
        string _,
        string firstStart,
        string firstEnd,
        string secondStart,
        string secondEnd,
        bool expected)
    {
        var date = new DateOnly(2026, 11, 2);
        var first = new TimeWindow(date.At(firstStart), date.At(firstEnd));
        var second = new TimeWindow(date.At(secondStart), date.At(secondEnd));

        Assert.Equal(expected, first.Overlaps(second));
    }

    [Fact]
    public void Constructor_RejectsEmptyOrReversedWindow()
    {
        var start = DateTimeOffset.Parse("2026-11-02T10:00:00Z");

        Assert.Throws<ArgumentException>(() => new TimeWindow(start, start));
        Assert.Throws<ArgumentException>(() => new TimeWindow(start, start.AddMinutes(-1)));

    }

    public static IEnumerable<object[]> WorkingHoursCases =>
    [
        ["inside", "09:00", "10:00", true],
        ["starts before open", "07:59", "09:00", false],
        ["ends after close", "16:30", "17:01", false],
        ["ends exactly at close", "16:00", "17:00", true]
    ];

    [Theory]
    [MemberData(nameof(WorkingHoursCases))]
    public void IsWithinWorkingHours_EnforcesOpenAndCloseBoundaries(
        string _,
        string start,
        string end,
        bool expected)
    {
        var date = new DateOnly(2026, 11, 2);
        var window = new TimeWindow(date.At(start), date.At(end));

        Assert.Equal(expected, BookingRules.IsWithinWorkingHours(
            window, TimeZoneInfo.Utc, new TimeOnly(8, 0), new TimeOnly(17, 0)));
    }

    [Fact]
    public void HasStarted_UsesTheInjectedTimeProvider()
    {
        var now = DateTimeOffset.Parse("2026-11-02T10:00:00Z");
        var clock = new FixedTimeProvider(now);

        Assert.True(BookingRules.HasStarted(
            new TimeWindow(now.AddMinutes(-1), now.AddMinutes(30)), clock));
        Assert.True(BookingRules.HasStarted(
            new TimeWindow(now, now.AddMinutes(30)), clock));
        Assert.False(BookingRules.HasStarted(
            new TimeWindow(now.AddMinutes(1), now.AddMinutes(30)), clock));
    }

    [Fact]
    public void SchedulerMetrics_RecordsBookingsConflictsAndDuration()
    {
        var counterMeasurements = new List<(string Name, long Value, KeyValuePair<string, object?>[] Tags)>();
        var histogramMeasurements = new List<(string Name, double Value)>();
        using var metrics = new SchedulerMetrics();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "ServiceScheduler")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            counterMeasurements.Add((instrument.Name, value, tags.ToArray())));
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) =>
            histogramMeasurements.Add((instrument.Name, value)));
        listener.Start();

        metrics.RecordAppointmentConfirmed();
        metrics.RecordBookingConflict("VEHICLE_ALREADY_BOOKED");
        metrics.RecordBookingDuration(12.5);

        Assert.Contains(counterMeasurements, measurement =>
            measurement.Name == "appointments_confirmed_total" && measurement.Value == 1);
        Assert.Contains(counterMeasurements, measurement =>
            measurement.Name == "booking_conflicts_total" && measurement.Value == 1 &&
            measurement.Tags.Any(tag => tag.Key == "reason" && Equals(tag.Value, "VEHICLE_ALREADY_BOOKED")));
        Assert.Contains(histogramMeasurements, measurement =>
            measurement.Name == "booking_duration_ms" && measurement.Value == 12.5);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_EchoesProvidedIdAndGeneratesMissingId()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var providedContext = new DefaultHttpContext();
        providedContext.Request.Headers["X-Correlation-Id"] = "test-correlation-id";

        await middleware.InvokeAsync(providedContext);

        Assert.Equal("test-correlation-id", providedContext.Response.Headers["X-Correlation-Id"]);

        var generatedContext = new DefaultHttpContext();
        await middleware.InvokeAsync(generatedContext);

        Assert.True(Guid.TryParseExact(
            generatedContext.Response.Headers["X-Correlation-Id"], "N", out _));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

internal static class DateOnlyTestExtensions
{
    public static DateTimeOffset At(this DateOnly date, string time) =>
        DateTimeOffset.Parse($"{date:yyyy-MM-dd}T{time}:00Z");
}
