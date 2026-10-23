namespace ServiceScheduler.Api.Domain;

public sealed record TimeWindow
{
    public TimeWindow(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new ArgumentException("The end of a time window must be after its start.", nameof(end));
        }

        Start = start.ToUniversalTime();
        End = end.ToUniversalTime();
    }

    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public bool Overlaps(TimeWindow other) => Start < other.End && End > other.Start;
}