using Microsoft.EntityFrameworkCore;
using Serilog;
using BookingMeetingRooms.Infrastructure.Data;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using BookingMeetingRooms.Api.Middleware;

namespace BookingMeetingRooms;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Налаштування Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .WriteTo.File("logs/booking-meeting-rooms-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Додавання сервісів
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
        });

        // FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();
        
        // Автентифікація (спрощена через headers)
        builder.Services.AddAuthentication("HeaderAuth")
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BookingMeetingRooms.Infrastructure.Middleware.HeaderAuthenticationHandler>("HeaderAuth", options => { });

        // Налаштування PostgreSQL та EF Core
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<AppDbContext>());

        // Налаштування BookingSettings
        builder.Services.Configure<BookingSettings>(
            builder.Configuration.GetSection("BookingSettings"));

        // Реєстрація сервісів
        builder.Services.AddScoped<IBookingConflictChecker, BookingConflictChecker>();
        builder.Services.AddScoped<ITimeSlotValidator, TimeSlotValidator>();

        // CORS
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

        // Налаштування HTTP pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseGlobalExceptionHandler();
        app.MapControllers();

        // Застосування міграцій при старті (тільки в Development)
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                    Log.Information("Database migrations applied");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply database migrations");
                }
            }
        }

        Log.Information("Application starting up");

        app.Run();
    }
}
