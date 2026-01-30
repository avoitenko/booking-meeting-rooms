using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.Events;

public class BookingCancelledEvent : DomainEvent
{
    public Guid BookingRequestId { get; }
    public Guid RoomId { get; }
    public Guid CancelledByUserId { get; }
    public string Reason { get; }

    public BookingCancelledEvent(
        Guid bookingRequestId,
        Guid roomId,
        Guid cancelledByUserId,
        string reason)
    {
        BookingRequestId = bookingRequestId;
        RoomId = roomId;
        CancelledByUserId = cancelledByUserId;
        Reason = reason;
    }
}
