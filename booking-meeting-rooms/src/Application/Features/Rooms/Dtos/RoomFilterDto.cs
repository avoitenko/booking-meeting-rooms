namespace BookingMeetingRooms.Application.Features.Rooms.Dtos;

public class RoomFilterDto
{
    public string? Location { get; set; }
    public int? MinCapacity { get; set; }
    public bool? IsActive { get; set; }
}
