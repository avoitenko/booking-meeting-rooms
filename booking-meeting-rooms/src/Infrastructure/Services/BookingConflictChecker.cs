using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Enums;
using BookingMeetingRooms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingMeetingRooms.Infrastructure.Services;

public class BookingConflictChecker : IBookingConflictChecker
{
    private readonly IApplicationDbContext _context;
    private readonly BookingSettings _settings;

    public BookingConflictChecker(
        IApplicationDbContext context,
        IOptions<BookingSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public async Task<bool> HasConflictAsync(
        BookingRequest bookingRequest,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default)
    {
        var conflictingStatuses = new List<BookingStatus> { BookingStatus.Confirmed };

        if (_settings.CheckSubmittedForConflicts)
        {
            conflictingStatuses.Add(BookingStatus.Submitted);
        }

        // Ефективна перевірка конфліктів через SQL запит
        var hasConflict = await _context.BookingRequests
            .Where(b => b.RoomId == bookingRequest.RoomId)
            .Where(b => b.Id != excludeBookingId)
            .Where(b => conflictingStatuses.Contains(b.Status))
            .Where(b => b.TimeSlot.StartAt < bookingRequest.TimeSlot.EndAt &&
                       b.TimeSlot.EndAt > bookingRequest.TimeSlot.StartAt)
            .AnyAsync(cancellationToken);

        return hasConflict;
    }
}
