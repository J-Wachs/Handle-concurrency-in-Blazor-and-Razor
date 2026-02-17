using HandleConcurrency.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HandleConcurrency.DesignTime;

/// <summary>
/// This class is ONLY used by Entity Framework Core's design-time tools (e.g., 'add-migration').
/// It provides the tools with a way to create a DbContext instance when the application is not running.
/// It is necessary because our Program.cs only registers the IDbContextFactory and not the DbContext itself.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        // Find the appsettings.{env}.json file:
        var currentDirectory = Directory.GetCurrentDirectory();

        // Search upwards for a directory containing our startup project.
        // This makes the script robust against different directory structures.
        var basePath = currentDirectory;

        // We start out by looking in 'HandleConcurrencyBlazorDemo':
        while (!Directory.Exists(Path.Combine(basePath, "HandleConcurrencyBlazorDemo")))
        {
            var parent = Directory.GetParent(basePath);
            if (parent is null)
            {
                throw new InvalidOperationException("Could not find the startup project's directory 'HandleConcurrencyBlazorDemo'.");
            }
            basePath = parent.FullName;
        }

        var startupProjectDir = Path.Combine(basePath, "HandleConcurrencyBlazorDemo");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(startupProjectDir) // Use the found path
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true) // Also good practice
            .Build();

        var builder = new DbContextOptionsBuilder<DatabaseContext>();
        var connectionString = configuration.GetConnectionString("Default");

        // An extra safeguard to provide a better error message
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find a connection string named 'Default' in appsettings.json.");
        }

        builder.UseSqlServer(connectionString);

        return new DatabaseContext(builder.Options);
    }
}
