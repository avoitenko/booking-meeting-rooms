using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class BookingStatusTransitionDto
{
    public int Id { get; set; }
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public int ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
