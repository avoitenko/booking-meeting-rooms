using FluentValidation;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;

namespace BookingMeetingRooms.Application.Features.Bookings.Validators;

public class CancelBookingDtoValidator : AbstractValidator<CancelBookingDto>
{
    public CancelBookingDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for cancellation")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}
