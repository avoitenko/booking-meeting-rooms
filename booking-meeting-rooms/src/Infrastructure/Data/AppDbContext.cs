using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingMeetingRooms.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<BookingRequest> BookingRequests { get; set; } = null!;
    public DbSet<BookingStatusTransition> BookingStatusTransitions { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
