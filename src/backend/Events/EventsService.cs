using Dapper;
using Npgsql;

namespace MeetupReservation.Api.Events;

public class EventsService
{
    private readonly string _connectionString;

    public EventsService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured");
    }

    public async Task<long> CreateEventAsync(long organizerId, CreateEventRequest req)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            var eventId = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO meetup.events (organizer_id, title, description, start_at, end_at, location, is_online, is_public, status)
                  VALUES (@OrganizerId, @Title, @Description, @StartAt, @EndAt, @Location, @IsOnline, @IsPublic, 'active')
                  RETURNING id",
                new
                {
                    OrganizerId = organizerId,
                    req.Title,
                    req.Description,
                    req.StartAt,
                    req.EndAt,
                    req.Location,
                    req.IsOnline,
                    req.IsPublic
                },
                tx);

            foreach (var tt in req.TicketTypes)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO meetup.ticket_types (event_id, name, price, capacity) VALUES (@EventId, @Name, @Price, @Capacity)",
                    new { EventId = eventId, tt.Name, tt.Price, tt.Capacity },
                    tx);
            }

            if (req.CategoryIds?.Length > 0)
            {
                foreach (var catId in req.CategoryIds)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO meetup.event_categories (event_id, category_id) VALUES (@EventId, @CategoryId)",
                        new { EventId = eventId, CategoryId = catId },
                        tx);
                }
            }

            tx.Commit();
            return eventId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(long id)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var evt = await conn.QueryFirstOrDefaultAsync<EventRow>(
            @"SELECT e.id, e.organizer_id, e.title, e.description, e.start_at, e.end_at, e.location, e.is_online, e.is_public, e.status, e.created_at,
                     op.name as organizer_name
              FROM meetup.events e
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE e.id = @Id AND e.is_public = true AND e.status != 'blocked'",
            new { Id = id });

        if (evt == null) return null;

        var categories = (await conn.QueryAsync<long>(
            "SELECT category_id FROM meetup.event_categories WHERE event_id = @EventId",
            new { EventId = id })).ToArray();

        var ticketTypes = (await conn.QueryAsync<TicketTypeDto>(
            "SELECT id, name, price, capacity FROM meetup.ticket_types WHERE event_id = @EventId",
            new { EventId = id })).ToArray();

        return new EventDto
        {
            Id = evt.id,
            OrganizerId = evt.organizer_id,
            OrganizerName = evt.organizer_name,
            Title = evt.title,
            Description = evt.description,
            StartAt = evt.start_at,
            EndAt = evt.end_at,
            Location = evt.location,
            IsOnline = evt.is_online,
            IsPublic = evt.is_public,
            Status = evt.status,
            CreatedAt = evt.created_at,
            CategoryIds = categories,
            TicketTypes = ticketTypes
        };
    }

    public async Task<EventDto?> GetEventByIdForOrganizerAsync(long id, long organizerId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var evt = await conn.QueryFirstOrDefaultAsync<EventRow>(
            @"SELECT e.id, e.organizer_id, e.title, e.description, e.start_at, e.end_at, e.location, e.is_online, e.is_public, e.status, e.created_at,
                     op.name as organizer_name
              FROM meetup.events e
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE e.id = @Id AND e.organizer_id = @OrganizerId",
            new { Id = id, OrganizerId = organizerId });

        if (evt == null) return null;

        var categories = (await conn.QueryAsync<long>(
            "SELECT category_id FROM meetup.event_categories WHERE event_id = @EventId",
            new { EventId = id })).ToArray();

        var ticketTypes = (await conn.QueryAsync<TicketTypeDto>(
            "SELECT id, name, price, capacity FROM meetup.ticket_types WHERE event_id = @EventId",
            new { EventId = id })).ToArray();

        return new EventDto
        {
            Id = evt.id,
            OrganizerId = evt.organizer_id,
            OrganizerName = evt.organizer_name,
            Title = evt.title,
            Description = evt.description,
            StartAt = evt.start_at,
            EndAt = evt.end_at,
            Location = evt.location,
            IsOnline = evt.is_online,
            IsPublic = evt.is_public,
            Status = evt.status,
            CreatedAt = evt.created_at,
            CategoryIds = categories,
            TicketTypes = ticketTypes
        };
    }

    public async Task<(string title, DateTime startAt, string? location)?> GetEventBasicInfoAsync(long eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<(string title, DateTime start_at, string? location)>(
            "SELECT title, start_at, location FROM meetup.events WHERE id = @Id",
            new { Id = eventId });
        return row.title != null ? (row.title, row.start_at, row.location) : null;
    }

    public async Task<bool> IsOrganizerOfEventAsync(long userId, long eventId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.events WHERE id = @EventId AND organizer_id = @UserId",
            new { EventId = eventId, UserId = userId });
        return count > 0;
    }

    public async Task<bool> CancelEventAsync(long eventId, long organizerId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.ExecuteAsync(
            "UPDATE meetup.events SET status = 'cancelled' WHERE id = @EventId AND organizer_id = @OrganizerId",
            new { EventId = eventId, OrganizerId = organizerId });
        return rows > 0;
    }

    public async Task<long> AddEventImageAsync(long eventId, long organizerId, byte[] content, string contentType, string fileName, int sortOrder)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT 1 FROM meetup.events WHERE id = @EventId AND organizer_id = @OrganizerId",
            new { EventId = eventId, OrganizerId = organizerId });
        if (exists == 0) throw new UnauthorizedAccessException("Event not found or access denied");

        var imageId = await conn.ExecuteScalarAsync<long>(
            "INSERT INTO meetup.event_images (event_id, content, content_type, file_name, sort_order) VALUES (@EventId, @Content, @ContentType, @FileName, @SortOrder) RETURNING id",
            new { EventId = eventId, Content = content, ContentType = contentType, FileName = fileName, SortOrder = sortOrder });
        return imageId;
    }

    public async Task<bool> IsOrganizerAsync(long userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.user_roles WHERE user_id = @UserId AND role = 'organizer'",
            new { UserId = userId });
        return count > 0;
    }

    public async Task<bool> CategoryExistsAsync(long categoryId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.categories WHERE id = @Id AND is_archived = false",
            new { Id = categoryId });
        return count > 0;
    }

    public async Task<EventsListResult> GetEventsListAsync(string? cursor, int limit, long[]? categoryIds, string? sortBy)
    {
        limit = Math.Clamp(limit, 1, 100);
        var sortByNorm = sortBy?.ToLowerInvariant() switch
        {
            "createdat" or "created_at" => "createdAt",
            _ => "startAt"
        };

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        (DateTime sortVal, long id)? cursorVal = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) && long.TryParse(parts[1], out var cid))
                cursorVal = (dt, cid);
        }

        var categoryFilter = categoryIds != null && categoryIds.Length > 0;
        var catClause = categoryFilter ? "AND EXISTS (SELECT 1 FROM meetup.event_categories ec WHERE ec.event_id = e.id AND ec.category_id = ANY(@CategoryIds))" : "";
        var cursorClauseCreated = cursorVal.HasValue ? "AND (e.created_at, e.id) < (@CursorSort, @CursorId)" : "";
        var cursorClauseStart = cursorVal.HasValue ? "AND (e.start_at, e.id) > (@CursorSort, @CursorId)" : "";

        var sql = sortByNorm == "createdAt"
            ? $@"SELECT e.id, e.organizer_id, e.title, e.start_at, e.end_at, e.location, e.is_online, e.status, e.created_at, op.name as organizer_name
               FROM meetup.events e
               LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
               WHERE e.is_public = true AND e.status != 'blocked' {catClause} {cursorClauseCreated}
               ORDER BY e.created_at DESC, e.id DESC
               LIMIT @Limit"
            : $@"SELECT e.id, e.organizer_id, e.title, e.start_at, e.end_at, e.location, e.is_online, e.status, e.created_at, op.name as organizer_name
               FROM meetup.events e
               LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
               WHERE e.is_public = true AND e.status != 'blocked' {catClause} {cursorClauseStart}
               ORDER BY e.start_at ASC, e.id ASC
               LIMIT @Limit";

        var fetchLimit = limit + 1;
        var events = (await conn.QueryAsync<EventListRow>(sql, new
        {
            CategoryIds = categoryIds ?? Array.Empty<long>(),
            CursorSort = cursorVal?.sortVal ?? default,
            CursorId = cursorVal?.id ?? 0L,
            Limit = fetchLimit
        })).ToList();

        var hasMore = events.Count > limit;
        if (hasMore) events.RemoveAt(events.Count - 1);

        string? nextCursor = null;
        if (hasMore && events.Count > 0)
        {
            var last = events[^1];
            var sortField = sortByNorm == "createdAt" ? last.created_at : last.start_at;
            nextCursor = $"{sortField:O}|{last.id}";
        }

        var categoryIdsPerEvent = new Dictionary<long, long[]>();
        if (events.Count > 0)
        {
            var ids = events.Select(e => e.id).ToArray();
            var cats = await conn.QueryAsync<(long event_id, long category_id)>(
                "SELECT event_id, category_id FROM meetup.event_categories WHERE event_id = ANY(@Ids)",
                new { Ids = ids });
            foreach (var g in cats.GroupBy(c => c.event_id))
                categoryIdsPerEvent[g.Key] = g.Select(c => c.category_id).ToArray();
        }

        var items = events.Select(e => new EventListItemDto
        {
            Id = e.id,
            OrganizerId = e.organizer_id,
            OrganizerName = e.organizer_name,
            Title = e.title,
            StartAt = e.start_at,
            EndAt = e.end_at,
            Location = e.location,
            IsOnline = e.is_online,
            Status = e.status,
            CreatedAt = e.created_at,
            CategoryIds = categoryIdsPerEvent.GetValueOrDefault(e.id, [])
        }).ToArray();

        return new EventsListResult(items, nextCursor);
    }

    public async Task<(EventListItemDto[] items, bool organizerExists)> GetOrganizerEventsAsync(long organizerId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var organizerExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.organizer_profiles WHERE user_id = @Id",
            new { Id = organizerId }) > 0;
        if (!organizerExists) return ([], false);

        var events = (await conn.QueryAsync<EventListRow>(
            @"SELECT e.id, e.organizer_id, e.title, e.start_at, e.end_at, e.location, e.is_online, e.status, e.created_at, op.name as organizer_name
              FROM meetup.events e
              LEFT JOIN meetup.organizer_profiles op ON op.user_id = e.organizer_id
              WHERE e.organizer_id = @OrganizerId AND e.is_public = true AND e.status != 'blocked'
              ORDER BY e.start_at ASC",
            new { OrganizerId = organizerId })).ToList();

        var ids = events.Select(e => e.id).ToArray();
        var cats = await conn.QueryAsync<(long event_id, long category_id)>(
            "SELECT event_id, category_id FROM meetup.event_categories WHERE event_id = ANY(@Ids)",
            new { Ids = ids });
        var categoryIdsPerEvent = cats.GroupBy(c => c.event_id).ToDictionary(g => g.Key, g => g.Select(c => c.category_id).ToArray());

        return (events.Select(e => new EventListItemDto
        {
            Id = e.id,
            OrganizerId = e.organizer_id,
            OrganizerName = e.organizer_name,
            Title = e.title,
            StartAt = e.start_at,
            EndAt = e.end_at,
            Location = e.location,
            IsOnline = e.is_online,
            Status = e.status,
            CreatedAt = e.created_at,
            CategoryIds = categoryIdsPerEvent.GetValueOrDefault(e.id, [])
        }).ToArray(), true);
    }

    public async Task<CategoryDto[]> GetCategoriesAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var items = await conn.QueryAsync<CategoryDto>(
            "SELECT id, name FROM meetup.categories WHERE is_archived = false ORDER BY sort_order, id");
        return items.ToArray();
    }

    public async Task<OrganizerProfileDto?> GetOrganizerProfileAsync(long organizerId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var profile = await conn.QueryFirstOrDefaultAsync<(string name, string? description, bool has_avatar)>(
            "SELECT name, description, (avatar_content IS NOT NULL) as has_avatar FROM meetup.organizer_profiles WHERE user_id = @UserId",
            new { UserId = organizerId });
        if (profile.name == null) return null;
        return new OrganizerProfileDto(organizerId, profile.name, profile.description, profile.has_avatar);
    }

    public async Task<(byte[] content, string contentType)?> GetOrganizerAvatarAsync(long organizerId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<(byte[]? content, string? content_type)>(
            "SELECT avatar_content, avatar_content_type FROM meetup.organizer_profiles WHERE user_id = @UserId AND avatar_content IS NOT NULL",
            new { UserId = organizerId });
        if (row.content == null || row.content_type == null) return null;
        return (row.content, row.content_type);
    }

    private record EventRow(long id, long organizer_id, string title, string? description, DateTime start_at, DateTime end_at, string? location, bool is_online, bool is_public, string status, DateTime created_at, string? organizer_name);
    private record EventListRow(long id, long organizer_id, string title, DateTime start_at, DateTime end_at, string? location, bool is_online, string status, DateTime created_at, string? organizer_name);
}

public record EventsListResult(EventListItemDto[] Items, string? NextCursor);

public record EventListItemDto
{
    public long Id { get; init; }
    public long OrganizerId { get; init; }
    public string? OrganizerName { get; init; }
    public string Title { get; init; } = "";
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? Location { get; init; }
    public bool IsOnline { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public long[] CategoryIds { get; init; } = [];
}

public record OrganizerProfileDto(long Id, string Name, string? Description, bool HasAvatar = false);

public record CategoryDto(long Id, string Name);

public record CreateEventRequest(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    string? Location,
    bool IsOnline,
    bool IsPublic,
    CreateTicketTypeRequest[] TicketTypes,
    long[]? CategoryIds);

public record CreateTicketTypeRequest(string Name, decimal Price, int Capacity);

public record EventDto
{
    public long Id { get; init; }
    public long OrganizerId { get; init; }
    public string? OrganizerName { get; init; }
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string? Location { get; init; }
    public bool IsOnline { get; init; }
    public bool IsPublic { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public long[] CategoryIds { get; init; } = [];
    public TicketTypeDto[] TicketTypes { get; init; } = [];
}

public record TicketTypeDto(long Id, string Name, decimal Price, int Capacity);
