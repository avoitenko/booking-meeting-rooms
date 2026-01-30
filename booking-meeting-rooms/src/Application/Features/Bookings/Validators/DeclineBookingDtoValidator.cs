using FluentValidation;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;

namespace BookingMeetingRooms.Application.Features.Bookings.Validators;

public class ReasonDtoValidator : AbstractValidator<ReasonDto>
{
    public ReasonDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
