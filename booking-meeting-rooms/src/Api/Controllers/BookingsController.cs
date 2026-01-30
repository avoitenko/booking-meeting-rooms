using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;
using BookingMeetingRooms.Application.Features.Bookings.Mappings;
using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Exceptions;
using System.Security.Claims;

namespace BookingMeetingRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IBookingConflictChecker _conflictChecker;
    private readonly ITimeSlotValidator _timeSlotValidator;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        IApplicationDbContext context,
        IBookingConflictChecker conflictChecker,
        ITimeSlotValidator timeSlotValidator,
        ILogger<BookingsController> logger)
    {
        _context = context;
        _conflictChecker = conflictChecker;
        _timeSlotValidator = timeSlotValidator;
        _logger = logger;
    }

    /// <summary>
    /// Створити запит на бронювання (Draft)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> CreateBooking(
        [FromBody] CreateBookingRequestDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID is required");
            }

            var room = await _context.Rooms.FindAsync(new object[] { dto.RoomId }, cancellationToken);
            if (room == null)
            {
                return NotFound($"Room with id {dto.RoomId} not found");
            }

            var timeSlot = _timeSlotValidator.ValidateAndCreate(dto.StartAt, dto.EndAt);

            var bookingRequest = new BookingRequest(
                room,
                timeSlot,
                dto.ParticipantEmails,
                dto.Description,
                userId.Value);

            _context.BookingRequests.Add(bookingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            // Завантажуємо Room для відображення
            bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingRequest.Id, cancellationToken);

            _logger.LogInformation("Booking request created: {BookingId} by user {UserId}", bookingRequest!.Id, userId);

            return CreatedAtAction(nameof(GetBooking), new { id = bookingRequest.Id }, bookingRequest.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating booking request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking request");
            return Problem("Failed to create booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Відправити запит на бронювання (Draft → Submitted)
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> SubmitBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID is required");
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound($"Booking request with id {id} not found");
            }

            if (bookingRequest.CreatedByUserId != userId.Value)
            {
                return Forbid("You can only submit your own booking requests");
            }

            // Перевірка конфліктів перед відправкою
            var hasConflict = await _conflictChecker.HasConflictAsync(bookingRequest, bookingRequest.Id, cancellationToken);
            if (hasConflict)
            {
                return Conflict("Booking conflict detected. Another booking exists for this room and time slot.");
            }

            bookingRequest.Submit();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking request submitted: {BookingId} by user {UserId}", bookingRequest.Id, userId);

            return Ok(bookingRequest.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation submitting booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting booking {BookingId}", id);
            return Problem("Failed to submit booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Підтвердити запит на бронювання (Submitted → Confirmed) - тільки для Admin
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingRequestDto>> ConfirmBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID is required");
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound($"Booking request with id {id} not found");
            }

            // Перевірка конфліктів перед підтвердженням
            var hasConflict = await _conflictChecker.HasConflictAsync(bookingRequest, bookingRequest.Id, cancellationToken);
            if (hasConflict)
            {
                return Conflict("Booking conflict detected. Another confirmed booking exists for this room and time slot.");
            }

            bookingRequest.Confirm(userId.Value);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking request confirmed: {BookingId} by admin {UserId}", bookingRequest.Id, userId);

            return Ok(bookingRequest.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation confirming booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict confirming booking {BookingId}", id);
            return Conflict("Booking was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking {BookingId}", id);
            return Problem("Failed to confirm booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Відхилити запит на бронювання (Submitted → Declined) - тільки для Admin
    /// </summary>
    [HttpPost("{id}/decline")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingRequestDto>> DeclineBooking(
        Guid id,
        [FromBody] DeclineBookingDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID is required");
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound($"Booking request with id {id} not found");
            }

            bookingRequest.Decline(userId.Value, dto.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking request declined: {BookingId} by admin {UserId}", bookingRequest.Id, userId);

            return Ok(bookingRequest.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation declining booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error declining booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining booking {BookingId}", id);
            return Problem("Failed to decline booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Скасувати підтверджене бронювання (Confirmed → Cancelled)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> CancelBooking(
        Guid id,
        [FromBody] CancelBookingDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User ID is required");
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound($"Booking request with id {id} not found");
            }

            if (bookingRequest.CreatedByUserId != userId.Value)
            {
                return Forbid("You can only cancel your own booking requests");
            }

            bookingRequest.Cancel(userId.Value, dto.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking request cancelled: {BookingId} by user {UserId}", bookingRequest.Id, userId);

            return Ok(bookingRequest.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation cancelling booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling booking {BookingId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return Problem("Failed to cancel booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Отримати деталі бронювання з історією переходів
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> GetBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound($"Booking request with id {id} not found");
            }

            return Ok(bookingRequest.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {BookingId}", id);
            return Problem("Failed to retrieve booking request", statusCode: 500);
        }
    }

    /// <summary>
    /// Пошук бронювань з фільтрацією
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<BookingRequestDto>>> GetBookings(
        [FromQuery] BookingFilterDto filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var query = _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .AsQueryable();

            // Employee бачить тільки свої бронювання
            if (!User.IsInRole("Admin"))
            {
                if (!userId.HasValue)
                {
                    return Unauthorized("User ID is required");
                }
                query = query.Where(b => b.CreatedByUserId == userId.Value);
            }

            if (filter.From.HasValue)
            {
                query = query.Where(b => b.TimeSlot.StartAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(b => b.TimeSlot.EndAt <= filter.To.Value);
            }

            if (filter.RoomId.HasValue)
            {
                query = query.Where(b => b.RoomId == filter.RoomId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(b => b.Status == filter.Status.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            return Ok(bookings.Select(b => b.ToDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings");
            return Problem("Failed to retrieve bookings", statusCode: 500);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
