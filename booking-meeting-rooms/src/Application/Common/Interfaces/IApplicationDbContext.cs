using BookingMeetingRooms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingMeetingRooms.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Room> Rooms { get; }
    DbSet<BookingRequest> BookingRequests { get; }
    DbSet<BookingStatusTransition> BookingStatusTransitions { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
