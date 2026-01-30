using FluentValidation;
using BookingMeetingRooms.Application.Features.Rooms.Dtos;

namespace BookingMeetingRooms.Application.Features.Rooms.Validators;

public class CreateRoomDtoValidator : AbstractValidator<CreateRoomDto>
{
    public CreateRoomDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MaximumLength(200).WithMessage("Room name must not exceed 200 characters");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Room capacity must be greater than 0");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Room location is required")
            .MaximumLength(200).WithMessage("Room location must not exceed 200 characters");
    }
}
