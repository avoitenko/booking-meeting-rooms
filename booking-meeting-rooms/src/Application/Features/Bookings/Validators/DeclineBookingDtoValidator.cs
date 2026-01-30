using FluentValidation;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;

namespace BookingMeetingRooms.Application.Features.Bookings.Validators;

public class DeclineBookingDtoValidator : AbstractValidator<DeclineBookingDto>
{
    public DeclineBookingDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for decline")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
