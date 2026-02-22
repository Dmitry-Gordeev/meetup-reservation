using Dapper;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Npgsql;

namespace MeetupReservation.Api.Controllers;

[ApiController]
public class SystemController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Health() => Ok();

    [HttpGet("api/v1/db/test")]
    public async Task<IActionResult> DbTest([FromServices] IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            return StatusCode(500, new { error = "Connection string not configured" });

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        var result = await conn.QueryFirstOrDefaultAsync<int>("SELECT 1");
        return Ok(new { value = result });
    }

    [HttpPost("api/v1/email/test")]
    public async Task<IActionResult> EmailTest([FromServices] IConfiguration config)
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

        return Ok(new { sent = true });
    }
}
