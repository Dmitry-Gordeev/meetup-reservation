using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeetupReservation.Api.Admin;

namespace MeetupReservation.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin)
    {
        _admin = admin;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var items = await _admin.GetEventsForModerationAsync();
        return Ok(items);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var items = await _admin.GetUsersAsync();
        return Ok(items);
    }

    [HttpPatch("events/{id:long}/block")]
    public async Task<IActionResult> BlockEvent(long id)
    {
        var result = await _admin.BlockEventAsync(id);
        if (result == null) return NotFound();
        if (result == false) return BadRequest(new { error = "Event is not active" });
        return Ok(new { status = "blocked" });
    }

    [HttpPatch("events/{id:long}/unblock")]
    public async Task<IActionResult> UnblockEvent(long id)
    {
        var result = await _admin.UnblockEventAsync(id);
        if (result == null) return NotFound();
        if (result == false) return BadRequest(new { error = "Event is not blocked" });
        return Ok(new { status = "active" });
    }

    [HttpPatch("organizers/{id:long}/block")]
    public async Task<IActionResult> BlockOrganizer(long id)
    {
        var result = await _admin.BlockOrganizerAsync(id);
        if (result == null) return NotFound();
        return Ok(new { status = "blocked" });
    }

    [HttpPatch("organizers/{id:long}/unblock")]
    public async Task<IActionResult> UnblockOrganizer(long id)
    {
        var result = await _admin.UnblockOrganizerAsync(id);
        if (result == null) return NotFound();
        return Ok(new { status = "unblocked" });
    }

    [HttpPatch("users/{id:long}/block")]
    public async Task<IActionResult> BlockUser(long id)
    {
        var result = await _admin.BlockUserAsync(id);
        if (result == null) return NotFound();
        return Ok(new { status = "blocked" });
    }

    [HttpPatch("users/{id:long}/unblock")]
    public async Task<IActionResult> UnblockUser(long id)
    {
        var result = await _admin.UnblockUserAsync(id);
        if (result == null) return NotFound();
        return Ok(new { status = "unblocked" });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var items = await _admin.GetCategoriesAsync();
        return Ok(items);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required" });

        var id = await _admin.CreateCategoryAsync(req.Name, req.SortOrder ?? 0);
        if (id == null) return Conflict(new { error = "Category with this name already exists" });
        return Created($"/api/v1/admin/categories/{id}", new { id });
    }

    [HttpPatch("categories/{id:long}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryRequestDto req)
    {
        var result = await _admin.UpdateCategoryAsync(id, req.Name, req.IsArchived, req.SortOrder);
        if (result == null) return NotFound();
        if (result == false) return BadRequest(new { error = "No changes or invalid data" });
        return Ok(new { status = "updated" });
    }
}

public record CreateCategoryRequestDto(string Name, int? SortOrder);
public record UpdateCategoryRequestDto(string? Name, bool? IsArchived, int? SortOrder);
