namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class CreateBookingRequestDto
{
    public Guid RoomId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public List<string> ParticipantEmails { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}
