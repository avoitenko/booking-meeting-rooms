using BookingMeetingRooms.Domain.Entities;

namespace BookingMeetingRooms.Application.Common.Interfaces;

public interface IBookingConflictChecker
{
    Task<bool> HasConflictAsync(
        BookingRequest bookingRequest,
        Guid? excludeBookingId = null,
        CancellationToken cancellationToken = default);
}
