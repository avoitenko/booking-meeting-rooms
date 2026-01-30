namespace BookingMeetingRooms.Infrastructure.Data;

public class BookingSettings
{
    public int MaxTimeSlotHours { get; set; } = 4;
    public bool CheckSubmittedForConflicts { get; set; } = false;
}
