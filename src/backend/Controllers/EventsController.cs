using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Notifications;
using MeetupReservation.Api.Registrations;
using static MeetupReservation.Api.Notifications.EmailTemplates;

namespace MeetupReservation.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class EventsController : ControllerBase
{
    private readonly EventsService _events;
    private readonly RegistrationsService _registrations;
    private readonly EmailService _email;

    public EventsController(EventsService events, RegistrationsService registrations, EmailService email)
    {
        _events = events;
        _registrations = registrations;
        _email = email;
    }

    [Authorize]
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest req)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        if (!await _events.IsOrganizerAsync(userId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Title is required" });

        if (req.TicketTypes == null || req.TicketTypes.Length == 0)
            return BadRequest(new { error = "At least one ticket type is required" });

        foreach (var tt in req.TicketTypes)
        {
            if (string.IsNullOrWhiteSpace(tt.Name) || tt.Capacity <= 0)
                return BadRequest(new { error = "Each ticket type must have name and capacity > 0" });
        }

        if (req.CategoryIds != null)
        {
            foreach (var catId in req.CategoryIds)
            {
                if (!await _events.CategoryExistsAsync(catId))
                    return BadRequest(new { error = $"Category {catId} not found or archived" });
            }
        }

        var eventId = await _events.CreateEventAsync(userId, req);
        return Created($"/api/v1/events/{eventId}", new { id = eventId });
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] string? cursor, [FromQuery] int? limit, [FromQuery] string? categoryIds, [FromQuery] string? sortBy)
    {
        var limitVal = limit ?? 20;
        long[]? catIds = null;
        if (!string.IsNullOrEmpty(categoryIds))
        {
            var parts = categoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var parsed = new List<long>();
            foreach (var p in parts)
                if (long.TryParse(p.Trim(), out var id)) parsed.Add(id);
            if (parsed.Count > 0) catIds = parsed.ToArray();
        }

        var result = await _events.GetEventsListAsync(cursor, limitVal, catIds, sortBy);
        return Ok(result);
    }

    [HttpGet("events/{id:long}")]
    public async Task<IActionResult> GetEventById(long id)
    {
        var evt = await _events.GetEventByIdAsync(id);
        return evt != null ? Ok(evt) : NotFound();
    }

    [HttpGet("organizers/{id:long}/events")]
    public async Task<IActionResult> GetOrganizerEvents(long id)
    {
        var (items, organizerExists) = await _events.GetOrganizerEventsAsync(id);
        if (!organizerExists) return NotFound();
        return Ok(items);
    }

    [HttpGet("organizers/{id:long}")]
    public async Task<IActionResult> GetOrganizerProfile(long id)
    {
        var profile = await _events.GetOrganizerProfileAsync(id);
        return profile != null ? Ok(profile) : NotFound();
    }

    [HttpGet("organizers/{id:long}/avatar")]
    public async Task<IActionResult> GetOrganizerAvatar(long id)
    {
        var avatar = await _events.GetOrganizerAvatarAsync(id);
        if (avatar == null) return NotFound();
        return File(avatar.Value.content, avatar.Value.contentType);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _events.GetCategoriesAsync();
        return Ok(categories);
    }

    [Authorize]
    [HttpPost("events/{id:long}/cancel")]
    public async Task<IActionResult> CancelEvent(long id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var cancelled = await _events.CancelEventAsync(id, userId);
        if (!cancelled) return NotFound();

        var evtInfo = await _events.GetEventBasicInfoAsync(id);
        var participants = await _registrations.GetEventParticipantsForNotificationAsync(id);
        if (evtInfo.HasValue)
        {
            foreach (var (participantEmail, firstName, lastName) in participants)
            {
                var name = $"{firstName} {lastName}".Trim();
                var body = EventCancelled(name, evtInfo.Value.title, evtInfo.Value.startAt);
                _ = _email.SendAsync(participantEmail, $"Отмена мероприятия: {evtInfo.Value.title}", body);
            }
        }

        return Ok(new { status = "cancelled" });
    }

    [Authorize]
    [HttpPost("events/{id:long}/images")]
    public async Task<IActionResult> UploadEventImage(long id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var form = await Request.ReadFormAsync();
        var file = form.Files.GetFile("image") ?? form.Files.FirstOrDefault();
        if (file == null)
            return BadRequest(new { error = "Image file is required" });

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var content = ms.ToArray();

        try
        {
            var contentType = file.ContentType ?? "application/octet-stream";
            var imageId = await _events.AddEventImageAsync(id, userId, content, contentType, file.FileName, 0);
            return Created($"/api/v1/events/{id}/images/{imageId}", new { id = imageId });
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
    }
}
