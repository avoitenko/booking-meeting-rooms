using Microsoft.EntityFrameworkCore;
using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Application.Common.Interfaces;

namespace BookingMeetingRooms.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<BookingRequest> BookingRequests { get; set; } = null!;
    public DbSet<BookingStatusTransition> BookingStatusTransitions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
