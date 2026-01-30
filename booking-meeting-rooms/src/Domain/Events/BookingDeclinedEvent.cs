using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.Events;

public class BookingDeclinedEvent : DomainEvent
{
    public Guid BookingRequestId { get; }
    public Guid RoomId { get; }
    public Guid DeclinedByUserId { get; }
    public string Reason { get; }

    public BookingDeclinedEvent(
        Guid bookingRequestId,
        Guid roomId,
        Guid declinedByUserId,
        string reason)
    {
        BookingRequestId = bookingRequestId;
        RoomId = roomId;
        DeclinedByUserId = declinedByUserId;
        Reason = reason;
    }
}
