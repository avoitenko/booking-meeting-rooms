using FluentValidation;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;
using System.Text.RegularExpressions;

namespace BookingMeetingRooms.Application.Features.Bookings.Validators;

public class CreateBookingRequestDtoValidator : AbstractValidator<CreateBookingRequestDto>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CreateBookingRequestDtoValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("Start time is required")
            .Must((dto, startAt) => startAt < dto.EndAt)
            .WithMessage("Start time must be before end time");

        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("End time is required")
            .Must((dto, endAt) => endAt > dto.StartAt)
            .WithMessage("End time must be after start time");

        RuleFor(x => x.ParticipantEmails)
            .NotEmpty().WithMessage("At least one participant is required")
            .Must(emails => emails != null && emails.Count > 0)
            .WithMessage("At least one participant is required");

        RuleForEach(x => x.ParticipantEmails)
            .Must(email => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
    }
}
