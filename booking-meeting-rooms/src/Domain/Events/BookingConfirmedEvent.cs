using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.Events;

public class BookingConfirmedEvent : DomainEvent
{
    public Guid BookingRequestId { get; }
    public Guid RoomId { get; }
    public DateTime StartAt { get; }
    public DateTime EndAt { get; }
    public Guid ConfirmedByUserId { get; }

    public BookingConfirmedEvent(
        Guid bookingRequestId,
        Guid roomId,
        DateTime startAt,
        DateTime endAt,
        Guid confirmedByUserId)
    {
        BookingRequestId = bookingRequestId;
        RoomId = roomId;
        StartAt = startAt;
        EndAt = endAt;
        ConfirmedByUserId = confirmedByUserId;
    }
}
