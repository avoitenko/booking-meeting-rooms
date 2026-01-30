using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Domain.Exceptions;

public class InvalidStatusTransitionException : DomainException
{
    public BookingStatus CurrentStatus { get; }
    public BookingStatus TargetStatus { get; }

    public InvalidStatusTransitionException(BookingStatus currentStatus, BookingStatus targetStatus)
        : base($"Cannot transition from {currentStatus} to {targetStatus}")
    {
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }
}
