using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.Events;

public class BookingSubmittedEvent : DomainEvent
{
    public Guid BookingRequestId { get; }
    public Guid RoomId { get; }
    public DateTime StartAt { get; }
    public DateTime EndAt { get; }
    public List<string> ParticipantEmails { get; }

    public BookingSubmittedEvent(
        Guid bookingRequestId,
        Guid roomId,
        DateTime startAt,
        DateTime endAt,
        List<string> participantEmails)
    {
        BookingRequestId = bookingRequestId;
        RoomId = roomId;
        StartAt = startAt;
        EndAt = endAt;
        ParticipantEmails = participantEmails;
    }
}
