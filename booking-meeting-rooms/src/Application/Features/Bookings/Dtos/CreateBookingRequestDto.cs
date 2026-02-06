using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BookingMeetingRooms.Application.Features.Bookings.Dtos;

public class CreateBookingRequestDto : IValidatableObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Required(ErrorMessage = "Room ID is required")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public DateTime EndAt { get; set; }

    [Required(ErrorMessage = "At least one participant is required")]
    [MinLength(1, ErrorMessage = "At least one participant is required")]
    public List<string> ParticipantEmails { get; set; } = new();

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt >= EndAt)
        {
            yield return new ValidationResult(
                "Start time must be before end time",
                new[] { nameof(StartAt), nameof(EndAt) });
        }

        if (ParticipantEmails != null)
        {
            for (int i = 0; i < ParticipantEmails.Count; i++)
            {
                var email = ParticipantEmails[i];
                if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
                {
                    yield return new ValidationResult(
                        "Invalid email format",
                        new[] { $"{nameof(ParticipantEmails)}[{i}]" });
                }
            }
        }
    }
}
