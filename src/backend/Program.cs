using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Npgsql;
using Dapper;

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

var app = builder.Build();

app.UseCors();

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

app.Run();
