using System.ComponentModel.DataAnnotations;

namespace BookingMeetingRooms.Application.Features.Rooms.Dtos;

public class UpdateRoomDto
{
    [MaxLength(200, ErrorMessage = "Room name must not exceed 200 characters")]
    public string? Name { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Room capacity must be greater than 0")]
    public int? Capacity { get; set; }

    [MaxLength(200, ErrorMessage = "Room location must not exceed 200 characters")]
    public string? Location { get; set; }

    public bool? IsActive { get; set; }
}
