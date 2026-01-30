using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Application.Features.Rooms.Dtos;
using BookingMeetingRooms.Application.Features.Rooms.Mappings;
using BookingMeetingRooms.Domain.Entities;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(Summary = "Створити кімнату", Description = "Створює нову переговорну кімнату. Доступно тільки для адміністраторів.")]
    [SwaggerResponse(201, "Кімната успішно створена", typeof(RoomDto))]
    [SwaggerResponse(400, "Помилка валідації")]
    [SwaggerResponse(401, "Не авторизовано")]
    [SwaggerResponse(403, "Немає прав доступу")]
    public async Task<ActionResult<RoomDto>> CreateRoom([FromBody] CreateRoomDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var room = dto.ToEntity();
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room created: {RoomId} by user {UserId}", room.Id, GetCurrentUserId());

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return Problem("Failed to create room", statusCode: 500);
        }
    }

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
                .OrderBy(r => r.Location)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);

            return Ok(rooms.Select(r => r.ToDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms");
            return Problem("Failed to retrieve rooms", statusCode: 500);
        }
    }

    /// <summary>
    /// Отримати кімнату за ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetRoom(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound($"Room with id {id} not found");
            }

            return Ok(room.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", id);
            return Problem("Failed to retrieve room", statusCode: 500);
        }
    }

    /// <summary>
    /// Оновити кімнату (тільки для Admin)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(
        Guid id,
        [FromBody] UpdateRoomDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound($"Room with id {id} not found");
            }

            room.Update(dto.Name, dto.Capacity, dto.Location, dto.IsActive);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room updated: {RoomId} by user {UserId}", room.Id, GetCurrentUserId());

            return Ok(room.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating room {RoomId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", id);
            return Problem("Failed to update room", statusCode: 500);
        }
    }

    /// <summary>
    /// Деактивувати кімнату (тільки для Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(new object[] { id }, cancellationToken);

            if (room == null)
            {
                return NotFound($"Room with id {id} not found");
            }

            room.Deactivate();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room deactivated: {RoomId} by user {UserId}", room.Id, GetCurrentUserId());

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", id);
            return Problem("Failed to delete room", statusCode: 500);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
