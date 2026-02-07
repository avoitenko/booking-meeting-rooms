using BookingMeetingRooms.Api.Middleware;
using BookingMeetingRooms.Application.Common.Interfaces;
using BookingMeetingRooms.Infrastructure.Data;
using BookingMeetingRooms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BookingMeetingRooms;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Перевіряємо, якщо перший аргумент - "migrate", запускаємо утиліту міграцій
        if (args.Length > 0 && args[0].ToLowerInvariant() == "migrate")
        {
            return await MigrationRunner.RunAsync(args);
        }

        // Інакше запускаємо звичайний веб-додаток
        // поточна папка додатку
        Environment.CurrentDirectory = AppContext.BaseDirectory;

        try
        {
            var builder = WebApplication.CreateBuilder(args);


            // Налаштування Serilog через вбудовану інтеграцію ASP.NET Core
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .WriteTo.Console()
                    .WriteTo.File(
                        path: "logs/.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff}\t{ThreadId}\t[{Level:u3}]\t{SourceContext}\t{Message} {NewLine}{Exception}")
                    .CreateLogger();
            }
            catch (Exception)
            {
                Console.WriteLine($"FATAL ERROR: logger is not initialized");
                throw;
            }

            builder.Host.UseSerilog();

            Log.Information("Starting application...");

            Log.Information("Configuring services...");

            // Додавання сервісів
            builder.Services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Включаємо автоматичну валідацію моделей для [ApiController]
                    // Повертаємо помилки валідації в уніфікованому форматі ErrorResponseDto
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errorResponse = Api.Dtos.ErrorResponseDto.FromModelState(context.ModelState);
                        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResponse);
                    };
                });
            Log.Information("Controllers configured");

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                // Додаємо можливість вводити кастомні заголовки в Swagger UI через параметри
                c.OperationFilter<Api.Filters.SwaggerHeaderOperationFilter>();
            });
            Log.Information("Swagger configured");

            // Автентифікація (спрощена через headers)
            builder.Services.AddAuthentication("HeaderAuth")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BookingMeetingRooms.Infrastructure.Middleware.HeaderAuthenticationHandler>("HeaderAuth", options => { });
            Log.Information("Authentication configured");

            // Налаштування PostgreSQL та EF Core
            Log.Information("Reading PostgreSQL configuration...");
            var postgreSqlConnection = builder.Configuration.GetSection("PostgreSqlConnection").Get<PostgreSqlConnection>();
            if (postgreSqlConnection == null)
            {
                Log.Fatal("PostgreSqlConnection configuration is missing");
                throw new InvalidOperationException("PostgreSqlConnection configuration is missing");
            }

            var connectionString = postgreSqlConnection.ToConnectionString();
            Log.Information("PostgreSQL connection string configured: Host={Host}, Port={Port}, Database={Database}",
                postgreSqlConnection.Host, postgreSqlConnection.Port, postgreSqlConnection.Database);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
            Log.Information("DbContext configured");

            builder.Services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());

            // Налаштування BookingSettings
            builder.Services.Configure<BookingSettings>(
                builder.Configuration.GetSection("BookingSettings"));
            Log.Information("BookingSettings configured");

            // Реєстрація сервісів
            builder.Services.AddScoped<IBookingConflictChecker, BookingConflictChecker>();
            builder.Services.AddScoped<ITimeSlotValidator, TimeSlotValidator>();
            Log.Information("Application services registered");

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
            Log.Information("CORS configured");

            // Налаштування Kestrel endpoints з опціональним HTTPS
            // Якщо HTTPS не налаштований в конфігурації, використовуємо тільки HTTP
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Перевіряємо, чи є HTTPS endpoint в конфігурації (опціонально)
                var kestrelSection = builder.Configuration.GetSection("Kestrel:Endpoints:Https");
                var enableHttps = kestrelSection.Exists() && !string.IsNullOrEmpty(kestrelSection.GetValue<string>("Url"));

                if (enableHttps)
                {
                    try
                    {
                        // Спробуємо налаштувати HTTPS endpoint з dev-сертификатом
                        options.ListenAnyIP(5001, listenOptions =>
                        {
                            listenOptions.UseHttps();
                        });
                        Log.Information("HTTPS endpoint configured on port 5001");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to configure HTTPS endpoint. Application will run on HTTP only.");
                        // Явно налаштовуємо тільки HTTP, якщо HTTPS не вдався
                        options.ListenAnyIP(5000);
                    }
                }
                else
                {
                    Log.Information("HTTPS endpoint not configured. Using HTTP only on port 5000.");
                    // Явно налаштовуємо HTTP endpoint
                    options.ListenAnyIP(5000);
                }
            });

            Log.Information("Building application...");
            var app = builder.Build();
            Log.Information("Application built successfully");

            // Налаштування HTTP pipeline
            Log.Information("Configuring HTTP pipeline...");

            // Swagger доступний завжди (не тільки в Development)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Meeting Rooms API v1");
                c.RoutePrefix = string.Empty; // Swagger UI буде доступний на кореневому шляху (http://localhost:5000/)
                c.DisplayRequestDuration(); // Показувати час виконання запитів
                c.EnableDeepLinking(); // Дозволити глибокі посилання
                c.EnableFilter(); // Дозволити фільтрацію endpoints
            });
            Log.Information("Swagger UI configured and available at root path");

            app.UseSerilogRequestLogging();

            // HTTPS редирект тільки якщо HTTPS endpoint налаштований в конфігурації
            var kestrelHttpsSection = app.Configuration.GetSection("Kestrel:Endpoints:Https");
            if (kestrelHttpsSection.Exists() && !string.IsNullOrEmpty(kestrelHttpsSection.GetValue<string>("Url")))
            {
                try
                {
                    app.UseHttpsRedirection();
                    Log.Information("HTTPS redirection enabled");
                }
                catch
                {
                    Log.Warning("HTTPS redirection disabled (HTTPS not available)");
                }
            }
            else
            {
                Log.Information("HTTPS redirection disabled (HTTPS endpoint not configured)");
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseGlobalExceptionHandler();
            app.MapControllers();
            Log.Information("HTTP pipeline configured");

            // Перевірка підключення до бази даних при старті
            Log.Information("Testing database connection...");
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // Перевіряємо підключення до бази даних
                    if (dbContext.Database.CanConnect())
                    {
                        Log.Information("Database connection successful!");

                        // Перевіряємо, чи база даних існує та доступна
                        var canConnectAsync = dbContext.Database.CanConnectAsync();
                        if (canConnectAsync.Result)
                        {
                            Log.Information("Database is accessible and ready");
                        }
                    }
                    else
                    {
                        Log.Warning("Cannot connect to database. Please check connection settings.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect to database. Exception type: {ExceptionType}, Message: {Message}",
                    ex.GetType().FullName, ex.Message);
                Log.Warning("Application will continue to start, but database operations may fail.");
            }

            Log.Information("Application starting up");

            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start. Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().FullName,
                ex.Message,
                ex.StackTrace);

            // Також виводимо в консоль для надійності
            Console.WriteLine($"FATAL ERROR: {ex.GetType().FullName}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"Inner Message: {ex.InnerException.Message}");
            }

            Log.CloseAndFlush();
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
