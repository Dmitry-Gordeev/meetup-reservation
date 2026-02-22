using Microsoft.AspNetCore.Mvc;
using MeetupReservation.Api.Auth;

namespace MeetupReservation.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required" });

        if (req.Role is not "organizer" and not "participant")
            return BadRequest(new { error = "Role must be 'organizer' or 'participant'" });

        if (req.Role == "organizer" && string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required for organizer" });

        if (req.Role == "participant" && (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName)))
            return BadRequest(new { error = "FirstName and LastName are required for participant" });

        if (await _auth.EmailExistsAsync(req.Email.Trim()))
            return Conflict(new { error = "Email already registered" });

        var passwordHash = AuthService.HashPassword(req.Password);
        var userId = await _auth.RegisterAsync(
            req.Email.Trim().ToLowerInvariant(),
            passwordHash,
            req.Role,
            req.Name?.Trim(),
            req.FirstName?.Trim(),
            req.LastName?.Trim());

        return Created("/api/v1/auth/register", new { id = userId, email = req.Email.Trim(), role = req.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required" });

        var user = await _auth.GetUserForLoginAsync(req.Email.Trim().ToLowerInvariant());
        if (user == null || !AuthService.VerifyPassword(req.Password, user.Value.passwordHash))
            return Unauthorized();

        var token = _auth.GenerateJwt(user.Value.userId, user.Value.email, user.Value.roles);
        return Ok(new { token });
    }
}

public record RegisterRequestDto(string Email, string Password, string Role, string? Name, string? FirstName, string? LastName);
public record LoginRequestDto(string Email, string Password);
