namespace ServiceScheduler.Api.Application;

public sealed record AvailabilitySlot(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool Available,
    int FreeBays,
    int FreeTechnicians);

public sealed record AvailabilityResult(IReadOnlyList<AvailabilitySlot> Slots, ServiceError? Error)
{
    public bool Succeeded => Error is null;

    public static AvailabilityResult Success(IReadOnlyList<AvailabilitySlot> slots) => new(slots, null);
    public static AvailabilityResult Failure(string code, ServiceErrorCategory category) =>
        new(Array.Empty<AvailabilitySlot>(), new(code, category));
}