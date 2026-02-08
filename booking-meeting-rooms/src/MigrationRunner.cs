using BookingMeetingRooms.Domain.Entities;
using BookingMeetingRooms.Domain.Enums;
using BookingMeetingRooms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using System.Data;
using System.Text;

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
        // Налаштування кодування консолі для відображення українських символів
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        
        // Для Windows: встановлюємо кодову сторінку UTF-8 (65001)
        if (OperatingSystem.IsWindows())
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Якщо не вдалося встановити, продовжуємо роботу
            }
        }

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
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: System.Globalization.CultureInfo.InvariantCulture)
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

            // Перевірка та створення бази даних, якщо вона не існує
            await EnsureDatabaseExistsAsync(postgreSqlConnection);

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

                    // Створення seed даних
                    await SeedDataAsync(dbContext);

                    Log.Information("Migration process completed successfully!");
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

    private static async Task EnsureDatabaseExistsAsync(PostgreSqlConnection postgreSqlConnection)
    {
        // Створюємо рядок підключення до системної бази даних (postgres)
        var masterConnectionString = $"Host={postgreSqlConnection.Host};Port={postgreSqlConnection.Port};Database=postgres;Username={postgreSqlConnection.Username};Password={postgreSqlConnection.Password}";

        try
        {
            Log.Information("Checking if database '{Database}' exists...", postgreSqlConnection.Database);

            await using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Перевіряємо, чи існує база даних
            var checkDbCommand = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @databaseName",
                connection);
            checkDbCommand.Parameters.AddWithValue("databaseName", postgreSqlConnection.Database);

            var databaseExists = await checkDbCommand.ExecuteScalarAsync() != null;

            if (!databaseExists)
            {
                Log.Information("Database '{Database}' does not exist. Creating it...", postgreSqlConnection.Database);

                // Створюємо базу даних
                // PostgreSQL не підтримує IF NOT EXISTS для CREATE DATABASE, тому перевіряємо вручну
                var createDbCommand = new NpgsqlCommand(
                    $"CREATE DATABASE \"{postgreSqlConnection.Database}\"",
                    connection);
                await createDbCommand.ExecuteNonQueryAsync();

                Log.Information("Database '{Database}' created successfully!", postgreSqlConnection.Database);
                Console.WriteLine($"SUCCESS: Database '{postgreSqlConnection.Database}' created successfully!");
            }
            else
            {
                Log.Information("Database '{Database}' already exists.", postgreSqlConnection.Database);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to ensure database exists. Exception type: {ExceptionType}, Message: {Message}",
                ex.GetType().FullName, ex.Message);
            Console.WriteLine($"ERROR: Failed to create database: {ex.Message}");
            throw;
        }
    }

    private static async Task SeedDataAsync(AppDbContext dbContext)
    {
        try
        {
            Log.Information("Checking if seed data is needed...");

            // Перевірка та створення користувачів
            var userCount = await dbContext.Users.CountAsync();
            if (userCount == 0)
            {
                Log.Information("Users table is empty, creating seed data...");
                var users = new[]
                {
                    new User("Адміністратор", "admin@company.com", UserRole.Admin),
                    new User("Співробітник 1", "employee1@company.com", UserRole.Employee),
                    new User("Співробітник 2", "employee2@company.com", UserRole.Employee)
                };

                await dbContext.Users.AddRangeAsync(users);
                await dbContext.SaveChangesAsync();
                Log.Information("Seed data: {Count} users created", users.Length);
                Console.WriteLine($"SUCCESS: Created {users.Length} users");
            }
            else
            {
                Log.Information("Users table already contains {Count} records, skipping seed", userCount);
            }

            // Перевірка та створення кімнат
            var roomCount = await dbContext.Rooms.CountAsync();
            if (roomCount == 0)
            {
                Log.Information("Rooms table is empty, creating seed data...");
                var rooms = new[]
                {
                    new Room("Конференц-зал A", 20, "Поверх 1", true),
                    new Room("Конференц-зал B", 15, "Поверх 1", true),
                    new Room("Мала переговорна", 6, "Поверх 2", true),
                    new Room("Велика переговорна", 30, "Поверх 2", true),
                    new Room("Кімната для переговорів", 10, "Поверх 3", true)
                };

                await dbContext.Rooms.AddRangeAsync(rooms);
                await dbContext.SaveChangesAsync();
                Log.Information("Seed data: {Count} rooms created", rooms.Length);
                Console.WriteLine($"SUCCESS: Created {rooms.Length} rooms");
            }
            else
            {
                Log.Information("Rooms table already contains {Count} records, skipping seed", roomCount);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to seed data. Exception type: {ExceptionType}, Message: {Message}",
                ex.GetType().FullName, ex.Message);
            // Не кидаємо виняток далі, щоб не перервати процес міграції
            Console.WriteLine($"WARNING: Failed to seed data: {ex.Message}");
        }
    }
}
