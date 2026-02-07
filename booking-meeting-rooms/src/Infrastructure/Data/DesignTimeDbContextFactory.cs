using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BookingMeetingRooms.Infrastructure.Data;

/// <summary>
/// Фабрика для створення DbContext під час design-time операцій EF Core (dotnet ef migrations add, dotnet ef database update)
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Визначаємо шлях до корневої папки проекту
        // DesignTimeDbContextFactory знаходиться в src/Infrastructure/Data/
        // appsettings.json знаходиться в корні проекту (там же де .csproj файл)
        
        var basePath = FindProjectRoot();
        
        if (string.IsNullOrEmpty(basePath) || !File.Exists(Path.Combine(basePath, "appsettings.json")))
        {
            throw new InvalidOperationException(
                $"Cannot find appsettings.json file. Searched in: {basePath ?? "unknown"}. " +
                $"Current directory: {Directory.GetCurrentDirectory()}. " +
                $"Please ensure appsettings.json exists in the project root directory.");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var postgreSqlConnection = configuration.GetSection("PostgreSqlConnection").Get<PostgreSqlConnection>();
        if (postgreSqlConnection == null)
        {
            throw new InvalidOperationException("PostgreSqlConnection configuration is missing in appsettings.json");
        }

        var connectionString = postgreSqlConnection.ToConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string? FindProjectRoot()
    {
        // Спробуємо знайти .csproj файл, який знаходиться в корні проекту
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;

        // Піднімаємося вгору по директоріях, шукаючи .csproj файл
        while (searchDir != null)
        {
            var csprojFiles = Directory.GetFiles(searchDir, "*.csproj");
            if (csprojFiles.Length > 0)
            {
                // Знайшли .csproj файл - це корінь проекту
                return searchDir;
            }

            var parent = Directory.GetParent(searchDir);
            if (parent == null || parent.FullName == searchDir)
            {
                break;
            }
            searchDir = parent.FullName;
        }

        // Якщо не знайшли через .csproj, пробуємо через Assembly location
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                // Assembly знаходиться в bin/Debug/net10.0/, піднімаємося на 4 рівні вгору
                var potentialRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", ".."));
                if (File.Exists(Path.Combine(potentialRoot, "appsettings.json")))
                {
                    return potentialRoot;
                }
            }
        }

        // Останній варіант - піднімаємося від поточної директорії
        var currentPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
        if (File.Exists(Path.Combine(currentPath, "appsettings.json")))
        {
            return currentPath;
        }

        return null;
    }
}
