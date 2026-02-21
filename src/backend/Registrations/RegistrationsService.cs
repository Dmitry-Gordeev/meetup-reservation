using Dapper;
using MeetupReservation.Api.Notifications;
using Npgsql;

namespace MeetupReservation.Api.Registrations;

public class RegistrationsService
{
    private readonly string _connectionString;
    private readonly EmailService _emailService;

    public RegistrationsService(IConfiguration config, EmailService emailService)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured");
        _emailService = emailService;
    }

    public async Task<RegistrationResult> CreateRegistrationAsync(long eventId, CreateRegistrationRequest req, long? userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var evt = await conn.QueryFirstOrDefaultAsync<(long id, string status)>(
            "SELECT id, status FROM meetup.events WHERE id = @EventId AND is_public = true",
            new { EventId = eventId });
        if (evt.id == 0 || evt.status != "active")
            return new RegistrationResult { Error = "Event not found or not available", StatusCode = 404 };

        var ticketType = await conn.QueryFirstOrDefaultAsync<(long id, decimal price, int capacity)>(
            "SELECT id, price, capacity FROM meetup.ticket_types WHERE id = @Id AND event_id = @EventId",
            new { Id = req.TicketTypeId, EventId = eventId });
        if (ticketType.id == 0)
            return new RegistrationResult { Error = "Ticket type not found", StatusCode = 400 };

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.registrations WHERE event_id = @EventId AND email = @Email AND status != 'cancelled'",
            new { EventId = eventId, Email = req.Email.Trim().ToLowerInvariant() });
        if (existing > 0)
            return new RegistrationResult { Error = "This email is already registered for this event", StatusCode = 409 };

        var registeredCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.registrations WHERE ticket_type_id = @TicketTypeId AND status IN ('registered', 'checked_in')",
            new { TicketTypeId = req.TicketTypeId });
        if (registeredCount >= ticketType.capacity)
            return new RegistrationResult { Error = "No places left for this ticket type", StatusCode = 400 };

        if (ticketType.price > 0 && req.PaymentCompleted != true)
            return new RegistrationResult { Error = "Payment required for paid tickets", StatusCode = 402 };

        long? finalUserId = userId;
        string firstName = req.FirstName?.Trim() ?? "";
        string lastName = req.LastName?.Trim() ?? "";
        string email = req.Email.Trim().ToLowerInvariant();
        string? phone = req.Phone?.Trim();

        if (userId.HasValue)
        {
            var profile = await conn.QueryFirstOrDefaultAsync<(string first_name, string last_name, string? middle_name, string email, string? phone)>(
                "SELECT first_name, last_name, middle_name, email, phone FROM meetup.participant_profiles WHERE user_id = @UserId",
                new { UserId = userId.Value });
            if (profile.first_name != null)
            {
                if (string.IsNullOrEmpty(firstName)) firstName = profile.first_name;
                if (string.IsNullOrEmpty(lastName)) lastName = profile.last_name;
                if (string.IsNullOrEmpty(email)) email = profile.email;
                if (string.IsNullOrEmpty(phone) && profile.phone != null) phone = profile.phone;
            }
        }

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email))
            return new RegistrationResult { Error = "First name, last name and email are required", StatusCode = 400 };

        var registrationId = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO meetup.registrations (event_id, ticket_type_id, user_id, email, first_name, last_name, middle_name, phone, status)
              VALUES (@EventId, @TicketTypeId, @UserId, @Email, @FirstName, @LastName, @MiddleName, @Phone, 'registered')
              RETURNING id",
            new
            {
                EventId = eventId,
                TicketTypeId = req.TicketTypeId,
                UserId = finalUserId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                MiddleName = req.MiddleName?.Trim(),
                Phone = phone
            });

        var evtInfo = await conn.QueryFirstOrDefaultAsync<(string title, DateTime start_at, string? location)>(
            "SELECT title, start_at, location FROM meetup.events WHERE id = @EventId",
            new { EventId = eventId });
        if (evtInfo.title != null)
        {
            var participantName = $"{firstName} {lastName}".Trim();
            var body = EmailTemplates.RegistrationConfirmation(participantName, evtInfo.title, evtInfo.start_at, evtInfo.location);
            _ = _emailService.SendAsync(email, $"Подтверждение регистрации: {evtInfo.title}", body);
        }

        return new RegistrationResult { Id = registrationId, StatusCode = 201 };
    }

    public async Task<long?> GetRegistrationEventIdAsync(long registrationId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<long?>(
            "SELECT event_id FROM meetup.registrations WHERE id = @Id",
            new { Id = registrationId });
    }

    public async Task<CancelRegistrationResult?> CancelRegistrationAsync(long registrationId, long? userId, bool isOrganizerOfEvent)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var reg = await conn.QueryFirstOrDefaultAsync<(long id, string email, string first_name, string last_name, long organizer_id, string event_title, DateTime start_at, string organizer_email, string? organizer_name)>(
            @"SELECT r.id, r.email, r.first_name, r.last_name, e.organizer_id, e.title, e.start_at, u.email as organizer_email, op.name as organizer_name
              FROM meetup.registrations r
              JOIN meetup.events e ON e.id = r.event_id
              JOIN meetup.users u ON u.id = e.organizer_id
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE r.id = @Id AND r.status IN ('registered', 'checked_in')",
            new { Id = registrationId });
        if (reg.id == 0) return null;

        var userEmail = userId.HasValue ? await GetUserEmailAsync(conn, userId.Value) : null;
        var isParticipant = userEmail != null && string.Equals(reg.email, userEmail, StringComparison.OrdinalIgnoreCase);
        if (!isOrganizerOfEvent && !isParticipant) return new CancelRegistrationResult { Forbidden = true };

        await conn.ExecuteAsync(
            "UPDATE meetup.registrations SET status = 'cancelled' WHERE id = @Id",
            new { Id = registrationId });

        if (isParticipant)
        {
            var organizerName = reg.organizer_name ?? "Организатор";
            var body = EmailTemplates.RegistrationCancelledByParticipant(
                organizerName,
                $"{reg.first_name} {reg.last_name}",
                reg.email,
                reg.event_title,
                reg.start_at);
            _ = _emailService.SendAsync(reg.organizer_email, $"Отмена регистрации: {reg.event_title}", body);
        }

        return new CancelRegistrationResult { Success = true };
    }

    private static async Task<string?> GetUserEmailAsync(NpgsqlConnection conn, long userId)
    {
        return await conn.ExecuteScalarAsync<string>("SELECT email FROM meetup.users WHERE id = @Id", new { Id = userId });
    }

    public async Task<(string email, string firstName, string lastName)[]> GetEventParticipantsForNotificationAsync(long eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<(string email, string first_name, string last_name)>(
            "SELECT email, first_name, last_name FROM meetup.registrations WHERE event_id = @EventId AND status IN ('registered', 'checked_in')",
            new { EventId = eventId });
        return rows.Select(r => (r.email, r.first_name, r.last_name)).ToArray();
    }

    public async Task<ParticipantProfileDto?> GetParticipantProfileAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var profile = await conn.QueryFirstOrDefaultAsync<(string first_name, string last_name, string? middle_name, string email, string? phone)>(
            "SELECT first_name, last_name, middle_name, email, phone FROM meetup.participant_profiles WHERE user_id = @UserId",
            new { UserId = userId });
        if (profile.first_name == null) return null;
        return new ParticipantProfileDto(
            profile.first_name,
            profile.last_name,
            profile.middle_name,
            profile.email,
            profile.phone);
    }

    /// <summary>
    /// WP-2.5.1: Регистрации залогиненного пользователя (по user_id или по email).
    /// </summary>
    public async Task<MyRegistrationDto[]> GetMyRegistrationsAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var userEmail = await GetUserEmailAsync(conn, userId);
        var rows = await conn.QueryAsync<(long id, long event_id, string event_title, DateTime start_at, string ticket_type_name, string status)>(
            @"SELECT r.id, r.event_id, e.title, e.start_at, tt.name, r.status
              FROM meetup.registrations r
              JOIN meetup.events e ON e.id = r.event_id
              JOIN meetup.ticket_types tt ON tt.id = r.ticket_type_id
              WHERE r.status IN ('registered', 'checked_in')
              AND (r.user_id = @UserId OR (r.user_id IS NULL AND LOWER(r.email) = LOWER(@UserEmail)))",
            new { UserId = userId, UserEmail = userEmail ?? "" });
        return rows.Select(r => new MyRegistrationDto(r.id, r.event_id, r.event_title, r.start_at, r.ticket_type_name, r.status)).ToArray();
    }

    /// <summary>
    /// WP-3.1.1: Ручная отметка чек-ина. Только организатор события.
    /// </summary>
    public async Task<CheckInResult> CheckInRegistrationAsync(long registrationId, long organizerUserId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var reg = await conn.QueryFirstOrDefaultAsync<(long id, long event_id, long organizer_id, string status)>(
            "SELECT r.id, r.event_id, e.organizer_id, r.status FROM meetup.registrations r JOIN meetup.events e ON e.id = r.event_id WHERE r.id = @Id",
            new { Id = registrationId });
        if (reg.id == 0) return new CheckInResult { NotFound = true };
        if (reg.organizer_id != organizerUserId) return new CheckInResult { Forbidden = true };
        if (reg.status == "checked_in") return new CheckInResult { Success = true }; // уже отмечен

        await conn.ExecuteAsync(
            "UPDATE meetup.registrations SET status = 'checked_in', checked_in_at = (NOW() AT TIME ZONE 'UTC') WHERE id = @Id",
            new { Id = registrationId });
        return new CheckInResult { Success = true };
    }

    /// <summary>
    /// WP-3.1.3: Список участников события. Только для организатора.
    /// </summary>
    public async Task<EventRegistrationDto[]?> GetEventRegistrationsForOrganizerAsync(long eventId, long organizerUserId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var isOrganizer = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.events WHERE id = @EventId AND organizer_id = @UserId",
            new { EventId = eventId, UserId = organizerUserId });
        if (isOrganizer == 0) return null;

        var rows = await conn.QueryAsync<(long id, string first_name, string last_name, string? middle_name, string email, string? phone, string status, DateTime? checked_in_at)>(
            @"SELECT id, first_name, last_name, middle_name, email, phone, status, checked_in_at
              FROM meetup.registrations
              WHERE event_id = @EventId AND status IN ('registered', 'checked_in')
              ORDER BY created_at",
            new { EventId = eventId });
        return rows.Select(r => new EventRegistrationDto(
            r.id,
            r.first_name,
            r.last_name,
            r.middle_name,
            r.email,
            r.phone,
            r.status,
            r.checked_in_at)).ToArray();
    }
}

public record CreateRegistrationRequest(
    long TicketTypeId,
    string Email,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string? Phone,
    bool? PaymentCompleted);

public record RegistrationResult(long? Id = null, string? Error = null, int StatusCode = 200);

public record ParticipantProfileDto(string FirstName, string LastName, string? MiddleName, string Email, string? Phone);

public record CancelRegistrationResult(bool Success = false, bool Forbidden = false);

public record MyRegistrationDto(long Id, long EventId, string EventTitle, DateTime StartAt, string TicketTypeName, string Status);

public record CheckInResult(bool Success = false, bool NotFound = false, bool Forbidden = false);

public record EventRegistrationDto(long Id, string FirstName, string LastName, string? MiddleName, string Email, string? Phone, string Status, DateTime? CheckedInAt);
