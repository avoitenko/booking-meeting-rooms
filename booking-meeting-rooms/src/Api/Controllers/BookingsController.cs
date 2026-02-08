using BookingMeetingRooms.Api.Dtos;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Application.Features.Bookings.Dtos;
using BookingMeetingRooms.Application.Features.Bookings.Mappings;
using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
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


    //+------------------------------------------------------------------+
    /// <summary>
    /// Пошук бронювань з фільтрацією
    /// </summary>
    [SwaggerOperation(Summary = "Пошук бронювань з фільтрацією")]
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
                    return Unauthorized(new ErrorResponseDto(
                        "Unauthorized",
                        "User ID is required",
                        "Please provide X-UserId header",
                        401));
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

            var bookingsList = bookings.Select(b => b.ToDto()).ToList();
            _logger.LogInformation("OPERATION: Booking requests retrieved successfully. Count={Count}, UserId={UserId}", 
                bookingsList.Count, GetCurrentUserId());

            return Ok(bookingsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings");
            return Problem("Failed to retrieve bookings", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Отримати деталі бронювання з історією переходів
    /// </summary>
    [SwaggerOperation(Summary = "Отримати деталі бронювання з історією переходів")]
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> GetBooking(
        int id,
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
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            return Ok(bookingRequest.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {BookingId}", id);
            return Problem("Failed to retrieve booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Створити запит на бронювання (Draft)
    /// </summary>
    /// <param name="dto">Дані для створення запиту на бронювання</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Створений запит на бронювання</returns>
    /// <response code="201">Запит успішно створено</response>
    /// <response code="400">Помилка валідації</response>
    /// <response code="401">Не авторизовано</response>
    /// <response code="404">Кімната не знайдена</response>
    [HttpPost]
    [Authorize]
    [SwaggerOperation(Summary = "Створити запит на бронювання", Description = "Створює новий запит на бронювання кімнати зі статусом Draft. Потрібна авторизація через заголовок X-UserId.")]
    [SwaggerResponse(201, "Запит успішно створено", typeof(BookingRequestDto))]
    [SwaggerResponse(400, "Помилка валідації")]
    [SwaggerResponse(401, "Не авторизовано")]
    [SwaggerResponse(404, "Кімната не знайдена")]
    public async Task<ActionResult<BookingRequestDto>> CreateBooking(
        [FromBody] CreateBookingRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponseDto.FromModelState(ModelState));
        }

        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            var room = await _context.Rooms.FindAsync(new object[] { dto.RoomId }, cancellationToken);
            if (room == null)
            {
                return NotFound(new ErrorResponseDto(
                    "RoomNotFound",
                    $"Room with id {dto.RoomId} not found",
                    null,
                    404));
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

            // Перезавантажуємо з Room для маппінгу в DTO
            var createdBooking = await _context.BookingRequests
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingRequest.Id, cancellationToken);

            if (createdBooking == null)
            {
                _logger.LogError("Failed to retrieve created booking request with id {BookingId}", bookingRequest.Id);
                return Problem("Failed to retrieve created booking request", statusCode: 500);
            }

            _logger.LogInformation("Booking request created: {BookingId} by user {UserId}", createdBooking.Id, userId);
            _logger.LogInformation("OPERATION: Booking request created successfully. BookingId={BookingId}, RoomId={RoomId}, RoomName={RoomName}, StartAt={StartAt}, EndAt={EndAt}, Status={Status}, UserId={UserId}", 
                createdBooking.Id, createdBooking.RoomId, createdBooking.Room.Name, createdBooking.TimeSlot.StartAt, createdBooking.TimeSlot.EndAt, createdBooking.Status, userId.Value);

            return CreatedAtAction(nameof(GetBooking), new { id = createdBooking.Id }, createdBooking.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating booking request");
            return BadRequest(new ErrorResponseDto(
                "ValidationError",
                ex.Message,
                null,
                400));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating booking request. Inner exception: {InnerException}", ex.InnerException?.Message);
            return BadRequest(new ErrorResponseDto(
                "DatabaseError",
                "Failed to save booking request to database",
                ex.InnerException?.Message ?? ex.Message,
                400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking request. Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().FullName, ex.Message, ex.StackTrace);
            return BadRequest(new ErrorResponseDto(
                "Error",
                "Failed to create booking request",
                ex.Message,
                500));
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Відправити запит на бронювання (Draft → Submitted) - тільки для Employee (свої бронювання)
    /// </summary>
    [SwaggerOperation(Summary = "Відправити запит на бронювання")]
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> SubmitBooking(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            _logger.LogInformation(
                "Submit booking check: CurrentUserId={CurrentUserId}, BookingCreatedByUserId={BookingCreatedByUserId}, BookingId={BookingId}",
                userId.Value, bookingRequest.CreatedByUserId, id);

            if (bookingRequest.CreatedByUserId != userId.Value)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to submit booking {BookingId} created by user {CreatedByUserId}",
                    userId.Value, id, bookingRequest.CreatedByUserId);
                return StatusCode(403, new ErrorResponseDto(
                    "Forbidden",
                    "You can only submit your own booking requests",
                    $"Booking was created by user {bookingRequest.CreatedByUserId}, but you are user {userId.Value}",
                    403));
            }

            // Використовуємо транзакцію з рівнем ізоляції Serializable для запобігання race condition
            await using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Перезавантажуємо bookingRequest в межах транзакції для отримання актуального RowVersion
                bookingRequest = await _context.BookingRequests
                    .Include(b => b.Room)
                    .Include(b => b.StatusTransitions)
                    .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (bookingRequest == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new ErrorResponseDto(
                        "BookingNotFound",
                        $"Booking request with id {id} not found",
                        null,
                        404));
                }

                // Перевірка конфліктів перед відправкою (в межах транзакції)
                var hasConflict = await _conflictChecker.HasConflictAsync(bookingRequest, bookingRequest.Id, cancellationToken);
                if (hasConflict)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict(new ErrorResponseDto(
                        "BookingConflict",
                        "Booking conflict detected. Another booking exists for this room and time slot.",
                        null,
                        409));
                }

                // Отримуємо інформацію про користувача для формування опису операції
                var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
                var operationDescription = user != null 
                    ? $"{user.Name} відправив запит на розгляд"
                    : $"Користувач (ID: {userId.Value}) відправив запит на розгляд";

                bookingRequest.Submit(operationDescription);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Booking request submitted: {BookingId} by user {UserId}", bookingRequest.Id, userId);
            _logger.LogInformation("OPERATION: Booking request submitted successfully. BookingId={BookingId}, FromStatus=Draft, ToStatus=Submitted, UserId={UserId}", 
                bookingRequest.Id, userId.Value);

            return Ok(bookingRequest.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict submitting booking {BookingId}", id);
            return Conflict(new ErrorResponseDto(
                "ConcurrencyConflict",
                "Booking was modified by another user. Please refresh and try again.",
                null,
                409));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation submitting booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "InvalidOperation",
                ex.Message,
                null,
                400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting booking {BookingId}", id);
            return Problem("Failed to submit booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Скасувати підтверджене бронювання (Confirmed → Cancelled) - тільки для Employee (свої бронювання)
    /// </summary>
    [SwaggerOperation(Summary = "Скасувати підтверджене бронювання")]
    [HttpPost("{id}/cancel")]
    [Authorize]
    public async Task<ActionResult<BookingRequestDto>> CancelBooking(
        int id,
        [FromBody] ReasonDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponseDto.FromModelState(ModelState));
        }

        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            if (bookingRequest.CreatedByUserId != userId.Value)
            {
                return StatusCode(403, new ErrorResponseDto(
                    "Forbidden",
                    "You can only cancel your own booking requests",
                    null,
                    403));
            }

            // Використовуємо транзакцію для атомарності операції
            await using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Перезавантажуємо bookingRequest в межах транзакції
                bookingRequest = await _context.BookingRequests
                    .Include(b => b.Room)
                    .Include(b => b.StatusTransitions)
                    .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (bookingRequest == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new ErrorResponseDto(
                        "BookingNotFound",
                        $"Booking request with id {id} not found",
                        null,
                        404));
                }

                if (bookingRequest.CreatedByUserId != userId.Value)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return StatusCode(403, new ErrorResponseDto(
                        "Forbidden",
                        "You can only cancel your own booking requests",
                        null,
                        403));
                }

                // Отримуємо інформацію про користувача для формування опису операції
                var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
                var operationDescription = user != null 
                    ? $"{user.Name} скасував бронювання"
                    : $"Користувач (ID: {userId.Value}) скасував бронювання";

                bookingRequest.Cancel(userId.Value, dto.Reason, operationDescription);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Booking request cancelled: {BookingId} by user {UserId}", bookingRequest.Id, userId);
            _logger.LogInformation("OPERATION: Booking request cancelled successfully. BookingId={BookingId}, FromStatus=Confirmed, ToStatus=Cancelled, Reason={Reason}, UserId={UserId}", 
                bookingRequest.Id, dto.Reason, userId.Value);

            return Ok(bookingRequest.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict cancelling booking {BookingId}", id);
            return Conflict(new ErrorResponseDto(
                "ConcurrencyConflict",
                "Booking was modified by another user. Please refresh and try again.",
                null,
                409));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation cancelling booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "InvalidOperation",
                ex.Message,
                null,
                400));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "ValidationError",
                ex.Message,
                null,
                400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return Problem("Failed to cancel booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Підтвердити запит на бронювання (Submitted → Confirmed) - тільки для Admin
    /// </summary>
    [SwaggerOperation(Summary = "Підтвердити запит на бронювання (тільки для Admin)")]
    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingRequestDto>> ConfirmBooking(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            // Використовуємо транзакцію з рівнем ізоляції Serializable для запобігання race condition
            await using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Перезавантажуємо bookingRequest в межах транзакції для отримання актуального RowVersion
                bookingRequest = await _context.BookingRequests
                    .Include(b => b.Room)
                    .Include(b => b.StatusTransitions)
                    .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (bookingRequest == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new ErrorResponseDto(
                        "BookingNotFound",
                        $"Booking request with id {id} not found",
                        null,
                        404));
                }

                // Перевірка конфліктів перед підтвердженням (в межах транзакції)
                var hasConflict = await _conflictChecker.HasConflictAsync(bookingRequest, bookingRequest.Id, cancellationToken);
                if (hasConflict)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Conflict(new ErrorResponseDto(
                        "BookingConflict",
                        "Booking conflict detected. Another confirmed booking exists for this room and time slot.",
                        null,
                        409));
                }

                // Отримуємо інформацію про користувача для формування опису операції
                var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
                var operationDescription = user != null 
                    ? $"{user.Name} підтвердив бронювання"
                    : $"Адміністратор (ID: {userId.Value}) підтвердив бронювання";

                bookingRequest.Confirm(userId.Value, operationDescription);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Booking request confirmed: {BookingId} by admin {UserId}", bookingRequest.Id, userId);
            _logger.LogInformation("OPERATION: Booking request confirmed successfully. BookingId={BookingId}, FromStatus=Submitted, ToStatus=Confirmed, UserId={UserId}", 
                bookingRequest.Id, userId.Value);

            return Ok(bookingRequest.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation confirming booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "InvalidOperation",
                ex.Message,
                null,
                400));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict confirming booking {BookingId}", id);
            return Conflict(new ErrorResponseDto(
                "ConcurrencyConflict",
                "Booking was modified by another user. Please refresh and try again.",
                null,
                409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming booking {BookingId}", id);
            return Problem("Failed to confirm booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Відхилити запит на бронювання (Submitted → Declined) - тільки для Admin
    /// </summary>
    [SwaggerOperation(Summary = "Відхилити запит на бронювання (тільки для Admin)")]
    [HttpPost("{id}/decline")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingRequestDto>> DeclineBooking(
        int id,
        [FromBody] ReasonDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponseDto.FromModelState(ModelState));
        }

        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            // Використовуємо транзакцію для атомарності операції
            await using var transaction = await _context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Перезавантажуємо bookingRequest в межах транзакції
                bookingRequest = await _context.BookingRequests
                    .Include(b => b.Room)
                    .Include(b => b.StatusTransitions)
                    .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

                if (bookingRequest == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return NotFound(new ErrorResponseDto(
                        "BookingNotFound",
                        $"Booking request with id {id} not found",
                        null,
                        404));
                }

                // Отримуємо інформацію про користувача для формування опису операції
                var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
                var operationDescription = user != null 
                    ? $"{user.Name} відхилив бронювання"
                    : $"Адміністратор (ID: {userId.Value}) відхилив бронювання";

                bookingRequest.Decline(userId.Value, dto.Reason, operationDescription);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Booking request declined: {BookingId} by admin {UserId}", bookingRequest.Id, userId);
            _logger.LogInformation("OPERATION: Booking request declined successfully. BookingId={BookingId}, FromStatus=Submitted, ToStatus=Declined, Reason={Reason}, UserId={UserId}", 
                bookingRequest.Id, dto.Reason, userId.Value);

            return Ok(bookingRequest.ToDto());
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict declining booking {BookingId}", id);
            return Conflict(new ErrorResponseDto(
                "ConcurrencyConflict",
                "Booking was modified by another user. Please refresh and try again.",
                null,
                409));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation declining booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "InvalidOperation",
                ex.Message,
                null,
                400));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error declining booking {BookingId}", id);
            return BadRequest(new ErrorResponseDto(
                "ValidationError",
                ex.Message,
                null,
                400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining booking {BookingId}", id);
            return Problem("Failed to decline booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Видалити бронювання за ID
    /// </summary>
    [SwaggerOperation(Summary = "Видалити бронювання за ID", Description = "Admin може видаляти будь-які бронювання. Employee може видаляти тільки свої чернетки (Draft).")]
    [HttpDelete("{id}")]
    [Authorize]
    [SwaggerResponse(200, "Бронювання успішно видалено")]
    [SwaggerResponse(400, "Неможливо видалити бронювання в поточному статусі")]
    [SwaggerResponse(401, "Не авторизовано")]
    [SwaggerResponse(403, "Немає прав доступу")]
    [SwaggerResponse(404, "Бронювання не знайдено")]
    [SwaggerResponse(409, "Неможливо видалити через залежності")]
    public async Task<ActionResult> DeleteBooking(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponseDto(
                    "Unauthorized",
                    "User ID is required",
                    "Please provide X-UserId header",
                    401));
            }

            // Перевіряємо роль користувача
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var isAdmin = string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase);

            var bookingRequest = await _context.BookingRequests
                .Include(b => b.Room)
                .Include(b => b.StatusTransitions)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (bookingRequest == null)
            {
                return NotFound(new ErrorResponseDto(
                    "BookingNotFound",
                    $"Booking request with id {id} not found",
                    null,
                    404));
            }

            // Перевірка прав доступу
            if (!isAdmin)
            {
                // Employee може видаляти тільки свої чернетки (Draft)
                if (bookingRequest.CreatedByUserId != userId.Value)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to delete booking {BookingId} created by user {CreatedByUserId}",
                        userId.Value, id, bookingRequest.CreatedByUserId);
                    return StatusCode(403, new ErrorResponseDto(
                        "Forbidden",
                        "You can only delete your own booking requests",
                        $"Booking was created by user {bookingRequest.CreatedByUserId}, but you are user {userId.Value}",
                        403));
                }

                if (bookingRequest.Status != BookingStatus.Draft)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to delete booking {BookingId} with status {Status}",
                        userId.Value, id, bookingRequest.Status);
                    return BadRequest(new ErrorResponseDto(
                        "InvalidOperation",
                        "You can only delete draft booking requests",
                        $"Booking status is {bookingRequest.Status}, but only Draft bookings can be deleted by employees",
                        400));
                }
            }

            // Отримуємо інформацію про користувача для логування
            var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
            var userName = user?.Name ?? $"User ID: {userId.Value}";

            var bookingId = bookingRequest.Id;
            var bookingStatus = bookingRequest.Status;
            var bookingRoomId = bookingRequest.RoomId;

            _context.BookingRequests.Remove(bookingRequest);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Booking request deleted: {BookingId} by {UserName} (ID: {UserId})", 
                bookingId, userName, userId.Value);
            _logger.LogInformation("OPERATION: Booking request deleted successfully. BookingId={BookingId}, RoomId={RoomId}, Status={Status}, UserId={UserId}", 
                bookingId, bookingRoomId, bookingStatus, userId.Value);

            return Ok(new { message = $"Booking request with id {id} has been deleted successfully" });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("foreign key") == true || 
                                          ex.InnerException?.Message?.Contains("constraint") == true)
        {
            _logger.LogWarning(ex, "Cannot delete booking {BookingId} due to database constraints", id);
            return Conflict(new ErrorResponseDto(
                "CannotDeleteBooking",
                $"Cannot delete booking request with id {id} due to database constraints",
                ex.InnerException?.Message,
                409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting booking {BookingId}", id);
            return Problem("Failed to delete booking request", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
