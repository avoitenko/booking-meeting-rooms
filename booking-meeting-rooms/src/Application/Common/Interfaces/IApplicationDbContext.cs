using Microsoft.EntityFrameworkCore;
using BookingMeetingRooms.Domain.Entities;

namespace BookingMeetingRooms.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Room> Rooms { get; }
    DbSet<BookingRequest> BookingRequests { get; }
    DbSet<BookingStatusTransition> BookingStatusTransitions { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
