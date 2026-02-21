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
using MeetupReservation.Api.Auth;

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

builder.Services.AddSingleton<AuthService>(sp => new AuthService(sp.GetRequiredService<IConfiguration>()));

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
builder.Services.AddAuthorization();

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

app.Run();

record RegisterRequest(string Email, string Password, string Role, string? Name, string? FirstName, string? LastName);
record LoginRequest(string Email, string Password);
