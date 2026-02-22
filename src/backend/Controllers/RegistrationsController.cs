using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Export;
using MeetupReservation.Api.Registrations;

namespace MeetupReservation.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class RegistrationsController : ControllerBase
{
    private readonly RegistrationsService _registrations;
    private readonly EventsService _events;
    private readonly ExportService _export;

    public RegistrationsController(RegistrationsService registrations, EventsService events, ExportService export)
    {
        _registrations = registrations;
        _events = events;
        _export = export;
    }

    [HttpPost("events/{id:long}/registrations")]
    public async Task<IActionResult> CreateRegistration(long id, [FromBody] CreateRegistrationRequest req)
    {
        long? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr != null && long.TryParse(userIdStr, out var uid))
            userId = uid;

        var result = await _registrations.CreateRegistrationAsync(id, req, userId);
        return result.StatusCode switch
        {
            201 => Created($"/api/v1/registrations/{result.Id}", new { id = result.Id }),
            404 => NotFound(new { error = result.Error }),
            409 => Conflict(new { error = result.Error }),
            402 => StatusCode(402, new { error = result.Error }),
            400 => BadRequest(new { error = result.Error }),
            _ => StatusCode(result.StatusCode, new { error = result.Error ?? "Error" })
        };
    }

    [Authorize]
    [HttpDelete("registrations/{id:long}")]
    public async Task<IActionResult> CancelRegistration(long id)
    {
        long? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr != null && long.TryParse(userIdStr, out var uid))
            userId = uid;

        var eventId = await _registrations.GetRegistrationEventIdAsync(id);
        var isOrganizer = userId.HasValue && eventId.HasValue && await _events.IsOrganizerOfEventAsync(userId.Value, eventId.Value);

        var result = await _registrations.CancelRegistrationAsync(id, userId, isOrganizer);
        if (result == null) return NotFound();
        if (result.Forbidden) return Forbid();
        return Ok(new { status = "cancelled" });
    }

    [Authorize]
    [HttpPatch("registrations/{id:long}/check-in")]
    public async Task<IActionResult> CheckIn(long id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _registrations.CheckInRegistrationAsync(id, userId);
        if (result.NotFound) return NotFound();
        if (result.Forbidden) return Forbid();
        return Ok(new { status = "checked_in" });
    }

    [Authorize]
    [HttpGet("events/{id:long}/registrations")]
    public async Task<IActionResult> GetEventRegistrations(long id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var items = await _registrations.GetEventRegistrationsForOrganizerAsync(id, userId);
        if (items == null) return NotFound();
        return Ok(items);
    }

    [Authorize]
    [HttpGet("events/{id:long}/registrations/export")]
    public async Task<IActionResult> ExportRegistrations(long id, [FromQuery] string? format)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var fmt = format ?? "xlsx";
        var result = await _export.ExportAsync(id, userId, fmt);
        if (result == null) return NotFound();
        return File(result.Value.content, result.Value.contentType, result.Value.fileName);
    }
}
