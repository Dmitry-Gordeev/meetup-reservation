using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeetupReservation.Api.Registrations;

namespace MeetupReservation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    private readonly RegistrationsService _registrations;

    public MeController(RegistrationsService registrations)
    {
        _registrations = registrations;
    }

    [HttpGet]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        return Ok(new { id = userId, email, roles });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var profile = await _registrations.GetParticipantProfileAsync(userId);
        return profile != null ? Ok(profile) : NotFound();
    }

    [HttpGet("registrations")]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var items = await _registrations.GetMyRegistrationsAsync(userId);
        return Ok(items);
    }
}
