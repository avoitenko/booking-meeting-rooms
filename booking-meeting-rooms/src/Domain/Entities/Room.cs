using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.Entities;

public class Room : Entity
{
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Room() { }

    //+------------------------------------------------------------------+
    public Room(string name, int capacity, string location, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty", nameof(name));
        if (capacity <= 0)
            throw new ArgumentException("Room capacity must be greater than 0", nameof(capacity));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Room location cannot be empty", nameof(location));

        Name = name;
        Capacity = capacity;
        Location = location;
        IsActive = isActive;
    }

    //+------------------------------------------------------------------+
    public void Update(string name, int capacity, string location, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty", nameof(name));
        if (capacity <= 0)
            throw new ArgumentException("Room capacity must be greater than 0", nameof(capacity));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Room location cannot be empty", nameof(location));

        Name = name;
        Capacity = capacity;
        Location = location;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    //+------------------------------------------------------------------+
    public void UpdatePartial(string? name = null, int? capacity = null, string? location = null, bool? isActive = null)
    {
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Room name cannot be empty", nameof(name));
            Name = name;
        }

        if (capacity.HasValue)
        {
            if (capacity.Value <= 0)
                throw new ArgumentException("Room capacity must be greater than 0", nameof(capacity));
            Capacity = capacity.Value;
        }

        if (location != null)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new ArgumentException("Room location cannot be empty", nameof(location));
            Location = location;
        }

        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
