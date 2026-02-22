using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MeetupReservation.Api.Admin;
using MeetupReservation.Api.Auth;
using MeetupReservation.Api.Events;
using MeetupReservation.Api.Export;
using MeetupReservation.Api.Notifications;
using MeetupReservation.Api.Registrations;

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

builder.Services.AddControllers();
builder.Services.AddSingleton<EmailService>(sp => new EmailService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<AuthService>(sp => new AuthService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<EventsService>(sp => new EventsService(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<RegistrationsService>(sp => new RegistrationsService(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<EmailService>()));
builder.Services.AddSingleton<ExportService>(sp => new ExportService(sp.GetRequiredService<RegistrationsService>(), sp.GetRequiredService<EventsService>()));
builder.Services.AddSingleton<AdminService>(sp => new AdminService(
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<EmailService>(),
    sp.GetRequiredService<RegistrationsService>(),
    sp.GetRequiredService<EventsService>()));
builder.Services.AddHostedService<MeetupReservation.Api.Notifications.ReminderBackgroundService>();

var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MeetupReservation",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MeetupReservation",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
