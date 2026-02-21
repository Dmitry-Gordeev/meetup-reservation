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

app.Run();
