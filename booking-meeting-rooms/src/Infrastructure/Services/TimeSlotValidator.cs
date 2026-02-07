using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Domain.ValueObjects;
using BookingMeetingRooms.Infrastructure.Data;
using Microsoft.Extensions.Options;

namespace BookingMeetingRooms.Infrastructure.Services;

public class TimeSlotValidator : ITimeSlotValidator
{
    private readonly BookingSettings _settings;

    public TimeSlotValidator(IOptions<BookingSettings> settings)
    {
        _settings = settings.Value;
    }

    public TimeSlot ValidateAndCreate(DateTime startAt, DateTime endAt)
    {
        return new TimeSlot(startAt, endAt, _settings.MaxTimeSlotHours);
    }
}
