using Dapper;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Notifications;
using MeetupReservation.Api.Registrations;
using Npgsql;

namespace MeetupReservation.Api.Admin;

/// <summary>
/// WP-3.3: Admin Module — блокировка, управление пользователями и категориями.
/// </summary>
public class AdminService
{
    private readonly string _connectionString;
    private readonly EmailService _emailService;
    private readonly RegistrationsService _registrations;
    private readonly EventsService _events;

    public AdminService(
        IConfiguration config,
        EmailService emailService,
        RegistrationsService registrations,
        EventsService events)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured");
        _emailService = emailService;
        _registrations = registrations;
        _events = events;
    }

    public async Task<bool> IsAdminAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.user_roles WHERE user_id = @UserId AND role = 'admin'",
            new { UserId = userId });
        return count > 0;
    }

    public async Task<bool?> BlockEventAsync(long eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            "UPDATE meetup.events SET status = 'blocked' WHERE id = @Id AND status = 'active'",
            new { Id = eventId });
        return rows > 0 ? true : await EventExistsAsync(conn, eventId) ? (bool?)false : null;
    }

    public async Task<AdminEventDto[]> GetEventsForModerationAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<(long id, long organizer_id, string title, DateTime start_at, string status, string? organizer_name)>(
            @"SELECT e.id, e.organizer_id, e.title, e.start_at, e.status, op.name
              FROM meetup.events e
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE e.status IN ('active', 'blocked')
              ORDER BY e.start_at ASC");
        return rows.Select(r => new AdminEventDto(r.id, r.organizer_id, r.title, r.start_at, r.status, r.organizer_name)).ToArray();
    }

    public async Task<bool?> UnblockEventAsync(long eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            "UPDATE meetup.events SET status = 'active' WHERE id = @Id AND status = 'blocked'",
            new { Id = eventId });
        return rows > 0 ? true : await EventExistsAsync(conn, eventId) ? (bool?)false : null;
    }

    public async Task<bool?> BlockOrganizerAsync(long organizerUserId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var hasOrganizerRole = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.user_roles WHERE user_id = @UserId AND role = 'organizer'",
            new { UserId = organizerUserId });
        if (hasOrganizerRole == 0) return null;

        await conn.ExecuteAsync("UPDATE meetup.users SET is_blocked = true WHERE id = @Id", new { Id = organizerUserId });

        var eventIds = (await conn.QueryAsync<long>(
            "SELECT id FROM meetup.events WHERE organizer_id = @OrganizerId AND status = 'active'",
            new { OrganizerId = organizerUserId })).ToList();

        foreach (var evtId in eventIds)
        {
            await conn.ExecuteAsync("UPDATE meetup.events SET status = 'cancelled' WHERE id = @Id", new { Id = evtId });
            var evtInfo = await _events.GetEventBasicInfoAsync(evtId);
            if (evtInfo.HasValue)
            {
                var participants = await _registrations.GetEventParticipantsForNotificationAsync(evtId);
                foreach (var (email, firstName, lastName) in participants)
                {
                    var name = $"{firstName} {lastName}".Trim();
                    var body = EmailTemplates.EventCancelled(name, evtInfo.Value.title, evtInfo.Value.startAt);
                    _ = _emailService.SendAsync(email, $"Отмена мероприятия: {evtInfo.Value.title}", body);
                }
            }
        }

        return true;
    }

    public async Task<bool?> UnblockOrganizerAsync(long organizerUserId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            "UPDATE meetup.users SET is_blocked = false WHERE id = @Id",
            new { Id = organizerUserId });
        return rows > 0 ? true : null;
    }

    public async Task<bool?> BlockUserAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var user = await conn.QueryFirstOrDefaultAsync<(long id, string email)>(
            "SELECT id, email FROM meetup.users WHERE id = @Id",
            new { Id = userId });
        if (user.id == 0) return null;

        await conn.ExecuteAsync("UPDATE meetup.users SET is_blocked = true WHERE id = @Id", new { Id = userId });

        var regs = await conn.QueryAsync<(long reg_id, string first_name, string last_name, string participant_email, string event_title, DateTime start_at, string organizer_email, string? organizer_name)>(
            @"SELECT r.id, r.first_name, r.last_name, r.email, e.title, e.start_at, u.email, op.name
              FROM meetup.registrations r
              JOIN meetup.events e ON e.id = r.event_id
              JOIN meetup.users u ON u.id = e.organizer_id
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE r.user_id = @UserId AND r.status IN ('registered', 'checked_in')",
            new { UserId = userId });

        foreach (var reg in regs)
        {
            await conn.ExecuteAsync("UPDATE meetup.registrations SET status = 'cancelled' WHERE id = @Id", new { Id = reg.reg_id });
            var organizerName = reg.organizer_name ?? "Организатор";
            var body = EmailTemplates.RegistrationCancelledByParticipant(
                organizerName,
                $"{reg.first_name} {reg.last_name}",
                reg.participant_email,
                reg.event_title,
                reg.start_at);
            _ = _emailService.SendAsync(reg.organizer_email, $"Отмена регистрации: {reg.event_title}", body);
        }

        return true;
    }

    public async Task<bool?> UnblockUserAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            "UPDATE meetup.users SET is_blocked = false WHERE id = @Id",
            new { Id = userId });
        return rows > 0 ? true : null;
    }

    public async Task<AdminUserDto[]> GetUsersAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<(long id, string email, bool is_blocked, string[]? roles)>(
            @"SELECT u.id, u.email, u.is_blocked,
                     (SELECT array_agg(role) FROM meetup.user_roles WHERE user_id = u.id) as roles
              FROM meetup.users u
              ORDER BY u.created_at DESC");
        return rows.Select(r => new AdminUserDto(r.id, r.email, r.is_blocked, r.roles ?? [])).ToArray();
    }

    public async Task<AdminCategoryDto[]> GetCategoriesAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<(long id, string name, bool is_archived, int sort_order)>(
            "SELECT id, name, is_archived, sort_order FROM meetup.categories ORDER BY sort_order, id");
        return rows.Select(r => new AdminCategoryDto(r.id, r.name, r.is_archived, r.sort_order)).ToArray();
    }

    public async Task<long?> CreateCategoryAsync(string name, int sortOrder = 0)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        try
        {
            return await conn.ExecuteScalarAsync<long>(
                "INSERT INTO meetup.categories (name, is_archived, sort_order) VALUES (@Name, false, @SortOrder) RETURNING id",
                new { Name = name.Trim(), SortOrder = sortOrder });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            return null;
        }
    }

    public async Task<bool?> UpdateCategoryAsync(long id, string? name, bool? isArchived, int? sortOrder)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var updates = new List<string>();
        var param = new DynamicParameters();
        param.Add("Id", id);

        if (name != null) { updates.Add("name = @Name"); param.Add("Name", name.Trim()); }
        if (isArchived.HasValue) { updates.Add("is_archived = @IsArchived"); param.Add("IsArchived", isArchived.Value); }
        if (sortOrder.HasValue) { updates.Add("sort_order = @SortOrder"); param.Add("SortOrder", sortOrder.Value); }

        if (updates.Count == 0) return true;

        var sql = $"UPDATE meetup.categories SET {string.Join(", ", updates)} WHERE id = @Id";
        var rows = await conn.ExecuteAsync(sql, param);
        return rows > 0 ? true : (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM meetup.categories WHERE id = @Id", new { Id = id }) > 0 ? (bool?)false : null);
    }

    private static async Task<bool> EventExistsAsync(NpgsqlConnection conn, long eventId)
    {
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM meetup.events WHERE id = @Id", new { Id = eventId }) > 0;
    }
}

public record AdminUserDto(long Id, string Email, bool IsBlocked, string[] Roles);
public record AdminCategoryDto(long Id, string Name, bool IsArchived, int SortOrder);
public record AdminEventDto(long Id, long OrganizerId, string Title, DateTime StartAt, string Status, string? OrganizerName);
