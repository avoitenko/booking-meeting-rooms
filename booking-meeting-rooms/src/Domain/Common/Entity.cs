namespace BookingMeetingRooms.Domain.Common;

public abstract class Entity
{
    public int Id { get; protected set; }
    
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; protected set; }
    
    protected Entity() { }
}
