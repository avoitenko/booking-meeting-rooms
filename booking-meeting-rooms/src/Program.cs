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
            .WriteTo.File("logs/{Date}.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();

        // Додавання сервісів
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            
            // Додаємо можливість вводити кастомні заголовки в Swagger UI через параметри
            c.OperationFilter<Api.Filters.SwaggerHeaderOperationFilter>();
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
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Meeting Rooms API v1");
            c.RoutePrefix = string.Empty; // Swagger UI буде доступний на кореневому шляху
        });

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseGlobalExceptionHandler();
        app.MapControllers();

        // Застосування міграцій та seed даних при старті (тільки в Development)
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                try
                {
                    dbContext.Database.Migrate();
                    Log.Information("Database migrations applied");

                    // Seed дані - створення прикладних кімнат, якщо таблиця порожня
                    if (!dbContext.Rooms.Any())
                    {
                        var rooms = new[]
                        {
                            new Domain.Entities.Room("Конференц-зал A", 20, "Поверх 1", true),
                            new Domain.Entities.Room("Конференц-зал B", 15, "Поверх 1", true),
                            new Domain.Entities.Room("Мала переговорна", 6, "Поверх 2", true),
                            new Domain.Entities.Room("Велика переговорна", 30, "Поверх 2", true),
                            new Domain.Entities.Room("Кімната для переговорів", 10, "Поверх 3", true)
                        };

                        dbContext.Rooms.AddRange(rooms);
                        dbContext.SaveChanges();
                        Log.Information("Seed data: {Count} rooms created", rooms.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply database migrations or seed data");
                }
            }
        }

        Log.Information("Application starting up");

        app.Run();
    }
}
