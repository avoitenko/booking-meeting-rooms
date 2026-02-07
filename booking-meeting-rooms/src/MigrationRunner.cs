using BookingMeetingRooms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BookingMeetingRooms;

/// <summary>
/// Утиліта для застосування міграцій бази даних без запуску веб-сервера.
/// Використання: dotnet booking-meeting-rooms.dll migrate
/// Або: booking-meeting-rooms.exe migrate
/// </summary>
public class MigrationRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        // Перевіряємо аргумент команди
        if (args.Length == 0 || args[0].ToLowerInvariant() != "migrate")
        {
            Console.WriteLine("Usage: booking-meeting-rooms.exe migrate");
            Console.WriteLine("   or: dotnet booking-meeting-rooms.dll migrate");
            return 1;
        }

        try
        {
            // Поточна папка додатку (як у Program.cs)
            Environment.CurrentDirectory = AppContext.BaseDirectory;

            // Налаштування логгера
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/migration-.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Starting migration runner...");

            // Налаштування конфігурації
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Налаштування сервісів
            var services = new ServiceCollection();

            // Конфігурація PostgreSQL
            var postgreSqlConnection = configuration.GetSection("PostgreSqlConnection").Get<PostgreSqlConnection>();
            if (postgreSqlConnection == null)
            {
                Log.Fatal("PostgreSqlConnection configuration is missing");
                Console.WriteLine("ERROR: PostgreSqlConnection configuration is missing in appsettings.json");
                return 1;
            }

            var connectionString = postgreSqlConnection.ToConnectionString();
            Log.Information("PostgreSQL connection: Host={Host}, Port={Port}, Database={Database}",
                postgreSqlConnection.Host, postgreSqlConnection.Port, postgreSqlConnection.Database);

            // Реєстрація DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            var serviceProvider = services.BuildServiceProvider();

            // Застосування міграцій
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Log.Information("Checking database connection...");
                if (!await dbContext.Database.CanConnectAsync())
                {
                    Log.Error("Cannot connect to database. Please check connection settings.");
                    Console.WriteLine("ERROR: Cannot connect to database. Please check connection settings.");
                    return 1;
                }

                Log.Information("Database connection successful. Applying migrations...");

                try
                {
                    await dbContext.Database.MigrateAsync();
                    Log.Information("Migrations applied successfully!");
                    Console.WriteLine("SUCCESS: Database migrations applied successfully!");
                    return 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply migrations. Exception type: {ExceptionType}, Message: {Message}",
                        ex.GetType().FullName, ex.Message);
                    Console.WriteLine($"ERROR: Failed to apply migrations: {ex.Message}");
                    return 1;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Migration runner failed. Exception type: {ExceptionType}, Message: {Message}",
                ex.GetType().FullName, ex.Message);
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
