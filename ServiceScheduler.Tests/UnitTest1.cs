namespace ServiceScheduler.Tests;

public class UnitTest1
{
    [Fact]
    public void Overlaps_UsesHalfOpenBoundaries()
    {
        var first = new ServiceScheduler.Api.Domain.TimeWindow(
            DateTimeOffset.Parse("2026-11-02T10:00:00Z"),
            DateTimeOffset.Parse("2026-11-02T11:00:00Z"));
        var backToBack = new ServiceScheduler.Api.Domain.TimeWindow(
            DateTimeOffset.Parse("2026-11-02T11:00:00Z"),
            DateTimeOffset.Parse("2026-11-02T12:00:00Z"));
        var overlapping = new ServiceScheduler.Api.Domain.TimeWindow(
            DateTimeOffset.Parse("2026-11-02T10:59:00Z"),
            DateTimeOffset.Parse("2026-11-02T12:00:00Z"));

        Assert.False(first.Overlaps(backToBack));
        Assert.True(first.Overlaps(overlapping));
    }

    [Fact]
    public void Constructor_RejectsEmptyOrReversedWindow()
    {
        var start = DateTimeOffset.Parse("2026-11-02T10:00:00Z");

        Assert.Throws<ArgumentException>(() => new ServiceScheduler.Api.Domain.TimeWindow(start, start));
        Assert.Throws<ArgumentException>(() => new ServiceScheduler.Api.Domain.TimeWindow(start, start.AddMinutes(-1)));

    }
}
