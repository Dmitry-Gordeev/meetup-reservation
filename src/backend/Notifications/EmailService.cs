using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MeetupReservation.Api.Notifications;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Smtp:Host"] ?? "localhost";
        var port = _config.GetValue<int>("Smtp:Port", 1025);
        var useSsl = _config.GetValue<bool>("Smtp:UseSsl", false);
        var userName = _config["Smtp:UserName"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"] ?? "noreply@meetup-reservation.local";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
        if (!string.IsNullOrEmpty(userName))
            await client.AuthenticateAsync(userName, password ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
