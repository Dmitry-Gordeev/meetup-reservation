using Dapper;
using Npgsql;

namespace MeetupReservation.Api.Registrations;

public class RegistrationsService
{
    private readonly string _connectionString;

    public RegistrationsService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured");
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

        return new RegistrationResult { Id = registrationId, StatusCode = 201 };
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
