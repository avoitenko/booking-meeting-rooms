using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Application.Features.Rooms.Dtos;

namespace BookingMeetingRooms.Application.Features.Rooms.Mappings;

public static class RoomMapping
{
    public static RoomDto ToDto(this Room room)
    {
        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Capacity = room.Capacity,
            Location = room.Location,
            IsActive = room.IsActive,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    public static Room ToEntity(this CreateRoomDto dto)
    {
        return new Room(dto.Name, dto.Capacity, dto.Location, dto.IsActive);
    }
}
