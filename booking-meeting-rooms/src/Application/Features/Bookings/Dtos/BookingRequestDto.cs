using BookingMeetingRooms.Domain.Enums;

namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class BookingRequestDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomLocation { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<string> ParticipantEmails { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BookingStatusTransitionDto> StatusTransitions { get; set; } = new();
}
