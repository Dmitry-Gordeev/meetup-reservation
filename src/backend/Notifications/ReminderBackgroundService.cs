using Dapper;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Registrations;
using Npgsql;

namespace MeetupReservation.Api.Notifications;

/// <summary>
/// WP-2.3: Фоновая задача для отправки напоминаний участникам за 24ч и за 1ч до мероприятия.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public ReminderBackgroundService(IServiceProvider services, ILogger<ReminderBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString)) return;

        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
        var registrations = scope.ServiceProvider.GetRequiredService<RegistrationsService>();
        var events = scope.ServiceProvider.GetRequiredService<EventsService>();

        var now = DateTime.UtcNow;

        // Окно 24ч: события, начинающиеся через 23ч–25ч
        var window24hStart = now.AddHours(23);
        var window24hEnd = now.AddHours(25);
        await SendRemindersForWindowAsync(
            connectionString, emailService, registrations, events,
            window24hStart, window24hEnd, "24h",
            EmailTemplates.Reminder24Hours,
            "Напоминание: завтра мероприятие",
            ct);

        // Окно 1ч: события, начинающиеся через 50мин–70мин
        var window1hStart = now.AddMinutes(50);
        var window1hEnd = now.AddMinutes(70);
        await SendRemindersForWindowAsync(
            connectionString, emailService, registrations, events,
            window1hStart, window1hEnd, "1h",
            EmailTemplates.Reminder1Hour,
            "Напоминание: мероприятие через 1 час",
            ct);
    }

    private async Task SendRemindersForWindowAsync(
        string connectionString,
        EmailService emailService,
        RegistrationsService registrations,
        EventsService events,
        DateTime windowStart,
        DateTime windowEnd,
        string reminderType,
        Func<string, string, DateTime, string?, string> templateFunc,
        string subjectPrefix,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var eventIds = (await conn.QueryAsync<long>(
            @"SELECT e.id FROM meetup.events e
              WHERE e.status = 'active' AND e.start_at >= @Start AND e.start_at <= @End
              AND NOT EXISTS (SELECT 1 FROM meetup.reminder_sent rs WHERE rs.event_id = e.id AND rs.reminder_type = @Type)",
            new { Start = windowStart, End = windowEnd, Type = reminderType })).ToList();

        foreach (var eventId in eventIds)
        {
            ct.ThrowIfCancellationRequested();
            var evtInfo = await events.GetEventBasicInfoAsync(eventId);
            if (!evtInfo.HasValue) continue;

            var participants = await registrations.GetEventParticipantsForNotificationAsync(eventId);
            if (participants.Length == 0) continue;

            foreach (var (email, firstName, lastName) in participants)
            {
                var name = $"{firstName} {lastName}".Trim();
                if (string.IsNullOrEmpty(name)) name = email;
                var body = templateFunc(name, evtInfo.Value.title, evtInfo.Value.startAt, evtInfo.Value.location);
                _ = emailService.SendAsync(email, $"{subjectPrefix}: {evtInfo.Value.title}", body, ct);
            }

            await conn.ExecuteAsync(
                "INSERT INTO meetup.reminder_sent (event_id, reminder_type) VALUES (@EventId, @Type) ON CONFLICT DO NOTHING",
                new { EventId = eventId, Type = reminderType });

            _logger.LogInformation("Sent {Type} reminders for event {EventId} to {Count} participants",
                reminderType, eventId, participants.Length);
        }
    }
}
