using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.ValueObjects;

public class TimeSlot : ValueObject
{
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }

    private TimeSlot() { }

    public TimeSlot(DateTime startAt, DateTime endAt, int maxHours)
    {
        if (endAt <= startAt)
            throw new ArgumentException("EndAt must be greater than StartAt", nameof(endAt));

        var duration = endAt - startAt;
        if (duration.TotalHours > maxHours)
            throw new ArgumentException($"Time slot duration cannot exceed {maxHours} hours", nameof(endAt));

        StartAt = startAt;
        EndAt = endAt;
    }

    public bool OverlapsWith(TimeSlot other)
    {
        return StartAt < other.EndAt && EndAt > other.StartAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartAt;
        yield return EndAt;
    }
}
