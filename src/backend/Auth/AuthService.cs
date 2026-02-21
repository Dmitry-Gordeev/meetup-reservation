using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Konscious.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace MeetupReservation.Api.Auth;

public class AuthService
{
    private readonly string _connectionString;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured");
        _jwtSecret = config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
        _jwtIssuer = config["Jwt:Issuer"] ?? "MeetupReservation";
        _jwtAudience = config["Jwt:Audience"] ?? "MeetupReservation";
    }

    public static string HashPassword(string password)
    {
        var salt = CreateSalt();
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            Iterations = 4,
            MemorySize = 65536
        };
        var hashBytes = argon2.GetBytes(64);
        var combined = new byte[80];
        Array.Copy(salt, 0, combined, 0, 16);
        Array.Copy(hashBytes, 0, combined, 16, 64);
        return Convert.ToBase64String(combined);
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var bytes = Convert.FromBase64String(stored);
        if (bytes.Length < 80) return false;
        var salt = new byte[16];
        Array.Copy(bytes, 0, salt, 0, 16);
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            Iterations = 4,
            MemorySize = 65536
        };
        var computed = argon2.GetBytes(64);
        var storedHash = new byte[64];
        Array.Copy(bytes, 16, storedHash, 0, 64);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }

    private static byte[] CreateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public async Task<long?> RegisterAsync(string email, string passwordHash, string role, string? name, string? firstName, string? lastName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        using var tx = conn.BeginTransaction();
        try
        {
            var userId = await conn.ExecuteScalarAsync<long>(
                "INSERT INTO meetup.users (email, password_hash, is_blocked) VALUES (@Email, @PasswordHash, false) RETURNING id",
                new { Email = email, PasswordHash = passwordHash },
                tx);

            await conn.ExecuteAsync(
                "INSERT INTO meetup.user_roles (user_id, role) VALUES (@UserId, @Role)",
                new { UserId = userId, Role = role },
                tx);

            if (role == "organizer")
            {
                await conn.ExecuteAsync(
                    "INSERT INTO meetup.organizer_profiles (user_id, name) VALUES (@UserId, @Name)",
                    new { UserId = userId, Name = name ?? email },
                    tx);
            }
            else if (role == "participant")
            {
                await conn.ExecuteAsync(
                    "INSERT INTO meetup.participant_profiles (user_id, first_name, last_name, email) VALUES (@UserId, @FirstName, @LastName, @Email)",
                    new { UserId = userId, FirstName = firstName ?? "", LastName = lastName ?? "", Email = email },
                    tx);
            }

            tx.Commit();
            return userId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<(long userId, string email, string passwordHash, string[] roles)?> GetUserForLoginAsync(string email)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var user = await conn.QueryFirstOrDefaultAsync<(long id, string email, string password_hash, bool is_blocked)>(
            "SELECT id, email, password_hash, is_blocked FROM meetup.users WHERE email = @Email",
            new { Email = email });

        if (user.id == 0 || user.is_blocked) return null;

        var roles = (await conn.QueryAsync<string>(
            "SELECT role FROM meetup.user_roles WHERE user_id = @UserId",
            new { UserId = user.id })).ToArray();

        return (user.id, user.email, user.password_hash, roles);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM meetup.users WHERE email = @Email",
            new { Email = email });
        return count > 0;
    }

    public string GenerateJwt(long userId, string email, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
