using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.ValueObjects;
using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Domain.Specifications;

public class BookingConflictSpecification
{
    private readonly bool _checkSubmitted;

    public BookingConflictSpecification(bool checkSubmitted)
    {
        _checkSubmitted = checkSubmitted;
    }

    public bool IsSatisfiedBy(
        BookingRequest newBooking,
        IEnumerable<BookingRequest> existingBookings)
    {
        var conflictingStatuses = new List<BookingStatus> { BookingStatus.Confirmed };

        if (_checkSubmitted)
        {
            conflictingStatuses.Add(BookingStatus.Submitted);
        }

        return existingBookings
            .Where(b => b.RoomId == newBooking.RoomId)
            .Where(b => conflictingStatuses.Contains(b.Status))
            .Any(b => b.TimeSlot.OverlapsWith(newBooking.TimeSlot));
    }
}
