using System.ComponentModel.DataAnnotations;

namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class ReasonDto
{
    [Required(ErrorMessage = "Reason is required")]
    [MaxLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
}
