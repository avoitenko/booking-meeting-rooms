namespace BookingMeetingRooms.Domain.Exceptions;

public class BookingConflictException : DomainException
{
    public Guid RoomId { get; }
    public DateTime StartAt { get; }
    public DateTime EndAt { get; }

    public BookingConflictException(Guid roomId, DateTime startAt, DateTime endAt)
        : base($"Booking conflict detected for room {roomId} between {startAt:yyyy-MM-dd HH:mm} and {endAt:yyyy-MM-dd HH:mm}")
    {
        RoomId = roomId;
        StartAt = startAt;
        EndAt = endAt;
    }
}
