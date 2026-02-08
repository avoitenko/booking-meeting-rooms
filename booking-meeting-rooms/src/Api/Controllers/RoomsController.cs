using BookingMeetingRooms.Api.Dtos;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Application.Features.Rooms.Dtos;
using BookingMeetingRooms.Application.Features.Rooms.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BookingMeetingRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IApplicationDbContext context,
        ILogger<RoomsController> logger)
    {
        _context = context;
        _logger = logger;
    }


    //+------------------------------------------------------------------+
    /// <summary>
    /// Створити кімнату (тільки для Admin)
    /// </summary>
    /// <param name="dto">Дані для створення кімнати</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Створена кімната</returns>
    /// <response code="201">Кімната успішно створена</response>
    /// <response code="400">Помилка валідації</response>
    /// <response code="401">Не авторизовано</response>
    /// <response code="403">Немає прав доступу (потрібна роль Admin)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Створити кімнату (тільки для Admin)", Description = "Створює нову переговорну кімнату. Доступно тільки для адміністраторів.")]
    [SwaggerResponse(201, "Кімната успішно створена", typeof(RoomDto))]
    [SwaggerResponse(400, "Помилка валідації")]
    [SwaggerResponse(401, "Не авторизовано")]
    [SwaggerResponse(403, "Немає прав доступу")]
    public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponseDto.FromModelState(ModelState));
        }

        try
        {
            var room = dto.ToEntity();
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room created: {RoomId} by user {UserId}", room.Id, GetCurrentUserId());
            _logger.LogInformation("OPERATION: Room created successfully. RoomId={RoomId}, Name={Name}, Capacity={Capacity}, Location={Location}, IsActive={IsActive}, UserId={UserId}", 
                room.Id, room.Name, room.Capacity, room.Location, room.IsActive, GetCurrentUserId());

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return Problem("Failed to create room", statusCode: 500);
        }
    }


    //+------------------------------------------------------------------+
    /// <summary>
    /// Отримати список кімнат з фільтрацією
    /// </summary>
    /// <param name="filter">Параметри фільтрації (Location, MinCapacity, IsActive)</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Список кімнат</returns>
    /// <response code="200">Список кімнат успішно отримано</response>
    [HttpGet]
    [SwaggerOperation(Summary = "Отримати список кімнат", Description = "Повертає список переговорних кімнат з можливістю фільтрації по локації, місткості та статусу активності.")]
    [SwaggerResponse(200, "Список кімнат", typeof(IEnumerable<RoomDto>))]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms(
        [FromQuery] RoomFilterDto filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Rooms.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                query = query.Where(r => r.Location.Contains(filter.Location));
            }

            if (filter.MinCapacity.HasValue)
            {
                query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(r => r.IsActive == filter.IsActive.Value);
            }

            var rooms = await query
                .OrderBy(r => r.Id)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var roomsList = rooms.Select(r => r.ToDto()).ToList();
            _logger.LogInformation("OPERATION: Rooms retrieved successfully. Count={Count}, UserId={UserId}", 
                roomsList.Count, GetCurrentUserId());

            return Ok(roomsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms");
            return Problem("Failed to retrieve rooms", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    /// <summary>
    /// Отримати кімнату за ID
    /// </summary>
    [SwaggerOperation(Summary = "Отримати кімнату за ID", Description = "Повертає переговорну кімнату по ID")]
    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetRoom(int id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound(new ErrorResponseDto(
                    "RoomNotFound",
                    $"Room with id {id} not found",
                    null,
                    404));
            }

            _logger.LogInformation("OPERATION: Room retrieved successfully. RoomId={RoomId}, Name={Name}, UserId={UserId}", 
                room.Id, room.Name, GetCurrentUserId());

            return Ok(room.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", id);
            return Problem("Failed to retrieve room", statusCode: 500);
        }
    }
    //+------------------------------------------------------------------+
    /// <summary>
    /// Оновити кімнату (тільки для Admin)
    /// </summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Оновити кімнату за ID (тільки для Admin)")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(
        int id,
        [FromBody] UpdateRoomDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ErrorResponseDto.FromModelState(ModelState));
        }

        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound(new ErrorResponseDto(
                    "RoomNotFound",
                    $"Room with id {id} not found",
                    null,
                    404));
            }

            // Часткове оновлення - оновлюємо тільки передані поля
            room.UpdatePartial(dto.Name, dto.Capacity, dto.Location, dto.IsActive);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room updated: {RoomId} by user {UserId}", room.Id, GetCurrentUserId());
            _logger.LogInformation("OPERATION: Room updated successfully. RoomId={RoomId}, Name={Name}, Capacity={Capacity}, Location={Location}, IsActive={IsActive}, UserId={UserId}", 
                room.Id, room.Name, room.Capacity, room.Location, room.IsActive, GetCurrentUserId());

            return Ok(room.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating room {RoomId}", id);
            return BadRequest(new ErrorResponseDto(
                "ValidationError",
                ex.Message,
                null,
                400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", id);
            return Problem("Failed to update room", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+

    /// <summary>
    /// Видалити кімнату (тільки для Admin)
    [SwaggerOperation(Summary = "Видалити кімнату за ID (тільки для Admin)")]
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(int id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound(new ErrorResponseDto(
                    "RoomNotFound",
                    $"Room with id {id} not found",
                    null,
                    404));
            }

            var roomName = room.Name;
            var roomId = room.Id;
            var userId = GetCurrentUserId();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room deleted: {RoomId} by user {UserId}", roomId, userId);
            _logger.LogInformation("OPERATION: Room deleted successfully. RoomId={RoomId}, Name={Name}, UserId={UserId}", 
                roomId, roomName, userId);

            return Ok(new { message = $"Room with id {id} has been deleted successfully" });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("foreign key") == true || ex.InnerException?.Message?.Contains("constraint") == true)
        {
            _logger.LogWarning(ex, "Cannot delete room {RoomId} because it has associated bookings", id);
            return Conflict(new ErrorResponseDto(
                "CannotDeleteRoom",
                $"Cannot delete room with id {id} because it has associated booking requests",
                "Please cancel or delete all booking requests for this room first",
                409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", id);
            return Problem("Failed to delete room", statusCode: 500);
        }
    }

    //+------------------------------------------------------------------+
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
