using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class BookingFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? RoomId { get; set; }
    public BookingStatus? Status { get; set; }
}
