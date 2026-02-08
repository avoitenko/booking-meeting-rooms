using BookingMeetingRooms.Application.Features.Bookings.Dtos;
using BookingMeetingRooms.Domain.Entities;

namespace BookingMeetingRooms.Application.Features.Bookings.Mappings;

public static class BookingRequestMapping
{
    public static BookingRequestDto ToDto(this BookingRequest bookingRequest)
    {
        return new BookingRequestDto
        {
            Id = bookingRequest.Id,
            RoomId = bookingRequest.RoomId,
            RoomName = bookingRequest.Room.Name,
            RoomLocation = bookingRequest.Room.Location,
            StartAt = bookingRequest.TimeSlot.StartAt,
            EndAt = bookingRequest.TimeSlot.EndAt,
            ParticipantEmails = new List<string>(bookingRequest.ParticipantEmails),
            Description = bookingRequest.Description,
            Status = bookingRequest.Status,
            CreatedByUserId = bookingRequest.CreatedByUserId,
            CreatedAt = bookingRequest.CreatedAt,
            UpdatedAt = bookingRequest.UpdatedAt,
            StatusTransitions = bookingRequest.StatusTransitions
                .OrderBy(t => t.CreatedAt)
                .Select(t => t.ToDto())
                .ToList()
        };
    }

    public static BookingStatusTransitionDto ToDto(this BookingStatusTransition transition)
    {
        return new BookingStatusTransitionDto
        {
            Id = transition.Id,
            FromStatus = transition.FromStatus,
            ToStatus = transition.ToStatus,
            ChangedByUserId = transition.ChangedByUserId,
            Reason = transition.Reason ?? string.Empty, // Завжди повертаємо рядок, навіть якщо причина не вказана
            CreatedAt = transition.CreatedAt
        };
    }
}
