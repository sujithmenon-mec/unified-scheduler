using System.Diagnostics.Metrics;

namespace ServiceScheduler.Api.Infrastructure;

public sealed class SchedulerMetrics : IDisposable
{
    private readonly Meter _meter = new("ServiceScheduler");
    private readonly Counter<long> _appointmentsConfirmed;
    private readonly Counter<long> _bookingConflicts;
    private readonly Histogram<double> _bookingDuration;

    public SchedulerMetrics()
    {
        _appointmentsConfirmed = _meter.CreateCounter<long>("appointments_confirmed_total");
        _bookingConflicts = _meter.CreateCounter<long>("booking_conflicts_total");
        _bookingDuration = _meter.CreateHistogram<double>("booking_duration_ms", "ms");
    }

    public void RecordAppointmentConfirmed() => _appointmentsConfirmed.Add(1);

    public void RecordBookingConflict(string reason) =>
        _bookingConflicts.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordBookingDuration(double durationMilliseconds) =>
        _bookingDuration.Record(durationMilliseconds);

    public void Dispose() => _meter.Dispose();
}
