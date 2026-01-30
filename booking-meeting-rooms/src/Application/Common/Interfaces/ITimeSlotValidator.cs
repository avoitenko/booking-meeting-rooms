using BookingMeetingRooms.Domain.ValueObjects;

namespace BookingMeetingRooms.Application.Common.Interfaces;

public interface ITimeSlotValidator
{
    TimeSlot ValidateAndCreate(DateTime startAt, DateTime endAt);
}
