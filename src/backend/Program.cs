using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using Npgsql;
using Dapper;
using MeetupReservation.Api.Admin;
using MeetupReservation.Api.Auth;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Export;
using MeetupReservation.Api.Notifications;
using static MeetupReservation.Api.Notifications.EmailTemplates;
using MeetupReservation.Api.Registrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<EmailService>(sp => new EmailService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<AuthService>(sp => new AuthService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<EventsService>(sp => new EventsService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<RegistrationsService>(sp => new RegistrationsService(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<EmailService>()));
builder.Services.AddSingleton<ExportService>(sp => new ExportService(sp.GetRequiredService<RegistrationsService>(), sp.GetRequiredService<EventsService>()));
builder.Services.AddSingleton<AdminService>(sp => new AdminService(
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<EmailService>(),
    sp.GetRequiredService<RegistrationsService>(),
    sp.GetRequiredService<EventsService>()));
builder.Services.AddHostedService<MeetupReservation.Api.Notifications.ReminderBackgroundService>();

var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MeetupReservation",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MeetupReservation",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok()).WithName("HealthCheck");

app.MapGet("/api/v1/db/test", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        return Results.Json(new { error = "Connection string not configured" }, statusCode: 500);

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    var result = await conn.QueryFirstOrDefaultAsync<int>("SELECT 1");
    return Results.Json(new { value = result });
});

app.MapPost("/api/v1/email/test", async (IConfiguration config) =>
{
    var host = config["Smtp:Host"] ?? "localhost";
    var port = config.GetValue<int>("Smtp:Port", 1025);
    var useSsl = config.GetValue<bool>("Smtp:UseSsl", false);
    var userName = config["Smtp:UserName"];
    var password = config["Smtp:Password"];

    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse("noreply@meetup-reservation.local"));
    message.To.Add(MailboxAddress.Parse("test@example.com"));
    message.Subject = "Meetup Reservation — тестовое письмо";
    message.Body = new TextPart("plain") { Text = "Тестовое письмо отправлено успешно." };

    using var client = new SmtpClient();
    await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
    if (!string.IsNullOrEmpty(userName))
        await client.AuthenticateAsync(userName, password ?? "");
    await client.SendAsync(message);
    await client.DisconnectAsync(true);

    return Results.Json(new { sent = true });
});

app.MapPost("/api/v1/auth/register", async (RegisterRequest req, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email and password are required" });

    if (req.Role is not "organizer" and not "participant")
        return Results.BadRequest(new { error = "Role must be 'organizer' or 'participant'" });

    if (req.Role == "organizer" && string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { error = "Name is required for organizer" });

    if (req.Role == "participant" && (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName)))
        return Results.BadRequest(new { error = "FirstName and LastName are required for participant" });

    if (await auth.EmailExistsAsync(req.Email.Trim()))
        return Results.Conflict(new { error = "Email already registered" });

    var passwordHash = AuthService.HashPassword(req.Password);
    var userId = await auth.RegisterAsync(
        req.Email.Trim().ToLowerInvariant(),
        passwordHash,
        req.Role,
        req.Name?.Trim(),
        req.FirstName?.Trim(),
        req.LastName?.Trim());

    return Results.Created("/api/v1/auth/register", new { id = userId, email = req.Email.Trim(), role = req.Role });
});

app.MapPost("/api/v1/auth/login", async (LoginRequest req, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { error = "Email and password are required" });

    var user = await auth.GetUserForLoginAsync(req.Email.Trim().ToLowerInvariant());
    if (user == null || !AuthService.VerifyPassword(req.Password, user.Value.passwordHash))
        return Results.Unauthorized();

    var token = auth.GenerateJwt(user.Value.userId, user.Value.email, user.Value.roles);
    return Results.Ok(new { token });
});

app.MapGet("/api/v1/me", [Microsoft.AspNetCore.Authorization.Authorize] (ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email);
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    return Results.Ok(new { id = userId, email, roles });
}).RequireAuthorization();

app.MapGet("/api/v1/me/profile", [Microsoft.AspNetCore.Authorization.Authorize] async (ClaimsPrincipal user, RegistrationsService registrations) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();
    var profile = await registrations.GetParticipantProfileAsync(userId);
    return profile != null ? Results.Ok(profile) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/v1/me/registrations", [Microsoft.AspNetCore.Authorization.Authorize] async (ClaimsPrincipal user, RegistrationsService registrations) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();
    var items = await registrations.GetMyRegistrationsAsync(userId);
    return Results.Ok(items);
}).RequireAuthorization();

app.MapPost("/api/v1/events", [Microsoft.AspNetCore.Authorization.Authorize] async (CreateEventRequest req, ClaimsPrincipal user, EventsService events) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    if (!await events.IsOrganizerAsync(userId))
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(req.Title))
        return Results.BadRequest(new { error = "Title is required" });

    if (req.TicketTypes == null || req.TicketTypes.Length == 0)
        return Results.BadRequest(new { error = "At least one ticket type is required" });

    foreach (var tt in req.TicketTypes)
    {
        if (string.IsNullOrWhiteSpace(tt.Name) || tt.Capacity <= 0)
            return Results.BadRequest(new { error = "Each ticket type must have name and capacity > 0" });
    }

    if (req.CategoryIds != null)
    {
        foreach (var catId in req.CategoryIds)
        {
            if (!await events.CategoryExistsAsync(catId))
                return Results.BadRequest(new { error = $"Category {catId} not found or archived" });
        }
    }

    var eventId = await events.CreateEventAsync(userId, req);
    return Results.Created($"/api/v1/events/{eventId}", new { id = eventId });
}).RequireAuthorization();

app.MapGet("/api/v1/events", async (string? cursor, int? limit, string? categoryIds, string? sortBy, EventsService events) =>
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
    var result = await events.GetEventsListAsync(cursor, limitVal, catIds, sortBy);
    return Results.Ok(result);
});

app.MapGet("/api/v1/events/{id:long}", async (long id, EventsService events) =>
{
    var evt = await events.GetEventByIdAsync(id);
    return evt != null ? Results.Ok(evt) : Results.NotFound();
});

app.MapGet("/api/v1/organizers/{id:long}/events", async (long id, EventsService events) =>
{
    var (items, organizerExists) = await events.GetOrganizerEventsAsync(id);
    if (!organizerExists) return Results.NotFound();
    return Results.Ok(items);
});

app.MapGet("/api/v1/organizers/{id:long}", async (long id, EventsService events) =>
{
    var profile = await events.GetOrganizerProfileAsync(id);
    return profile != null ? Results.Ok(profile) : Results.NotFound();
});

app.MapGet("/api/v1/organizers/{id:long}/avatar", async (long id, EventsService events) =>
{
    var avatar = await events.GetOrganizerAvatarAsync(id);
    if (avatar == null) return Results.NotFound();
    return Results.File(avatar.Value.content, avatar.Value.contentType);
});

app.MapGet("/api/v1/categories", async (EventsService events) =>
{
    var categories = await events.GetCategoriesAsync();
    return Results.Ok(categories);
});

app.MapPost("/api/v1/events/{id:long}/registrations", async (long id, CreateRegistrationRequest req, ClaimsPrincipal user, RegistrationsService registrations) =>
{
    long? userId = null;
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr != null && long.TryParse(userIdStr, out var uid))
        userId = uid;

    var result = await registrations.CreateRegistrationAsync(id, req, userId);
    return result.StatusCode switch
    {
        201 => Results.Created($"/api/v1/registrations/{result.Id}", new { id = result.Id }),
        404 => Results.NotFound(new { error = result.Error }),
        409 => Results.Conflict(new { error = result.Error }),
        402 => Results.Json(new { error = result.Error }, statusCode: 402),
        400 => Results.BadRequest(new { error = result.Error }),
        _ => Results.Json(new { error = result.Error ?? "Error" }, statusCode: result.StatusCode)
    };
});

app.MapPost("/api/v1/events/{id:long}/cancel", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, ClaimsPrincipal user, EventsService events, RegistrationsService registrations, EmailService email) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var cancelled = await events.CancelEventAsync(id, userId);
    if (!cancelled) return Results.NotFound();

    var evtInfo = await events.GetEventBasicInfoAsync(id);
    var participants = await registrations.GetEventParticipantsForNotificationAsync(id);
    if (evtInfo.HasValue)
    {
        foreach (var (participantEmail, firstName, lastName) in participants)
        {
            var name = $"{firstName} {lastName}".Trim();
            var body = EventCancelled(name, evtInfo.Value.title, evtInfo.Value.startAt);
            _ = email.SendAsync(participantEmail, $"Отмена мероприятия: {evtInfo.Value.title}", body);
        }
    }
    return Results.Ok(new { status = "cancelled" });
}).RequireAuthorization();

app.MapDelete("/api/v1/registrations/{id:long}", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, ClaimsPrincipal user, EventsService events, RegistrationsService registrations) =>
{
    long? userId = null;
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr != null && long.TryParse(userIdStr, out var uid))
        userId = uid;

    var eventId = await registrations.GetRegistrationEventIdAsync(id);
    var isOrganizer = userId.HasValue && eventId.HasValue && await events.IsOrganizerOfEventAsync(userId.Value, eventId.Value);

    var result = await registrations.CancelRegistrationAsync(id, userId, isOrganizer);
    if (result == null) return Results.NotFound();
    if (result.Forbidden) return Results.Forbid();
    return Results.Ok(new { status = "cancelled" });
}).RequireAuthorization();

app.MapPatch("/api/v1/registrations/{id:long}/check-in", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, ClaimsPrincipal user, RegistrationsService registrations) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var result = await registrations.CheckInRegistrationAsync(id, userId);
    if (result.NotFound) return Results.NotFound();
    if (result.Forbidden) return Results.Forbid();
    return Results.Ok(new { status = "checked_in" });
}).RequireAuthorization();

app.MapGet("/api/v1/events/{id:long}/registrations", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, ClaimsPrincipal user, RegistrationsService registrations) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var items = await registrations.GetEventRegistrationsForOrganizerAsync(id, userId);
    if (items == null) return Results.NotFound();
    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/api/v1/events/{id:long}/registrations/export", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, string? format, ClaimsPrincipal user, ExportService export) =>
{
    var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var fmt = format ?? "xlsx";
    var result = await export.ExportAsync(id, userId, fmt);
    if (result == null) return Results.NotFound();
    return Results.File(result.Value.content, result.Value.contentType, result.Value.fileName);
}).RequireAuthorization();

// WP-3.3: Admin endpoints (admin role only)
app.MapGet("/api/v1/admin/users", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (AdminService admin) =>
{
    var items = await admin.GetUsersAsync();
    return Results.Ok(items);
});

app.MapPatch("/api/v1/admin/events/{id:long}/block", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.BlockEventAsync(id);
    if (result == null) return Results.NotFound();
    if (result == false) return Results.BadRequest(new { error = "Event is not active" });
    return Results.Ok(new { status = "blocked" });
});

app.MapPatch("/api/v1/admin/events/{id:long}/unblock", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.UnblockEventAsync(id);
    if (result == null) return Results.NotFound();
    if (result == false) return Results.BadRequest(new { error = "Event is not blocked" });
    return Results.Ok(new { status = "active" });
});

app.MapPatch("/api/v1/admin/organizers/{id:long}/block", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.BlockOrganizerAsync(id);
    if (result == null) return Results.NotFound();
    return Results.Ok(new { status = "blocked" });
});

app.MapPatch("/api/v1/admin/organizers/{id:long}/unblock", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.UnblockOrganizerAsync(id);
    if (result == null) return Results.NotFound();
    return Results.Ok(new { status = "unblocked" });
});

app.MapPatch("/api/v1/admin/users/{id:long}/block", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.BlockUserAsync(id);
    if (result == null) return Results.NotFound();
    return Results.Ok(new { status = "blocked" });
});

app.MapPatch("/api/v1/admin/users/{id:long}/unblock", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, AdminService admin) =>
{
    var result = await admin.UnblockUserAsync(id);
    if (result == null) return Results.NotFound();
    return Results.Ok(new { status = "unblocked" });
});

app.MapGet("/api/v1/admin/categories", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (AdminService admin) =>
{
    var items = await admin.GetCategoriesAsync();
    return Results.Ok(items);
});

app.MapPost("/api/v1/admin/categories", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (CreateCategoryRequest req, AdminService admin) =>
{
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { error = "Name is required" });
    var id = await admin.CreateCategoryAsync(req.Name, req.SortOrder ?? 0);
    if (id == null) return Results.Conflict(new { error = "Category with this name already exists" });
    return Results.Created($"/api/v1/admin/categories/{id}", new { id });
});

app.MapPatch("/api/v1/admin/categories/{id:long}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = "AdminOnly")] async (long id, UpdateCategoryRequest req, AdminService admin) =>
{
    var result = await admin.UpdateCategoryAsync(id, req.Name, req.IsArchived, req.SortOrder);
    if (result == null) return Results.NotFound();
    if (result == false) return Results.BadRequest(new { error = "No changes or invalid data" });
    return Results.Ok(new { status = "updated" });
});

app.MapPost("/api/v1/events/{id:long}/images", [Microsoft.AspNetCore.Authorization.Authorize] async (long id, HttpContext http, EventsService events) =>
{
    var userIdStr = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
        return Results.Unauthorized();

    var form = await http.Request.ReadFormAsync();
    var file = form.Files.GetFile("image") ?? form.Files.FirstOrDefault();
    if (file == null)
        return Results.BadRequest(new { error = "Image file is required" });

    await using var stream = file.OpenReadStream();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    var content = ms.ToArray();

    try
    {
        var contentType = file.ContentType ?? "application/octet-stream";
        var imageId = await events.AddEventImageAsync(id, userId, content, contentType, file.FileName, 0);
        return Results.Created($"/api/v1/events/{id}/images/{imageId}", new { id = imageId });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.Run();

record RegisterRequest(string Email, string Password, string Role, string? Name, string? FirstName, string? LastName);
record LoginRequest(string Email, string Password);
record CreateCategoryRequest(string Name, int? SortOrder);
record UpdateCategoryRequest(string? Name, bool? IsArchived, int? SortOrder);
