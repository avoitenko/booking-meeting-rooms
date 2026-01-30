using BookingMeetingRooms.Domain.Common;
using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Domain.Entities;

public class BookingStatusTransition : Entity
{
    public Guid BookingRequestId { get; private set; }
    public BookingRequest BookingRequest { get; private set; } = null!;
    
    public BookingStatus FromStatus { get; private set; }
    public BookingStatus ToStatus { get; private set; }
    
    public Guid ChangedByUserId { get; private set; }
    
    public string? Reason { get; private set; }

    private BookingStatusTransition() { }

    public BookingStatusTransition(
        BookingRequest bookingRequest,
        BookingStatus fromStatus,
        BookingStatus toStatus,
        Guid changedByUserId,
        string? reason = null)
    {
        if (bookingRequest == null)
            throw new ArgumentNullException(nameof(bookingRequest));

        BookingRequestId = bookingRequest.Id;
        BookingRequest = bookingRequest;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        Reason = reason;
    }
}
