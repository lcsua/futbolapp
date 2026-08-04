using System;
using System.IO;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FootballManager.Infrastructure
{
    public class FootballManagerDbContextFactory : IDesignTimeDbContextFactory<FootballManagerDbContext>
    {
        public FootballManagerDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var basePath = ResolveConfigBasePath();
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Production.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No database connection string found for design-time. " +
                    "Set ConnectionStrings__DefaultConnection / DATABASE_CONNECTION, or provide appsettings.json.");
            }

            var builder = new DbContextOptionsBuilder<FootballManagerDbContext>();
            builder.UseNpgsql(connectionString);
            return new FootballManagerDbContext(builder.Options);
        }

        private static string ResolveConfigBasePath()
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Running from solution / backend folder during local development
            var apiPath = Path.Combine(currentDir, "FootballManager.Api");
            if (Directory.Exists(apiPath) && File.Exists(Path.Combine(apiPath, "appsettings.json")))
                return apiPath;

            var parent = Directory.GetParent(currentDir)?.FullName;
            if (parent != null)
            {
                apiPath = Path.Combine(parent, "FootballManager.Api");
                if (Directory.Exists(apiPath) && File.Exists(Path.Combine(apiPath, "appsettings.json")))
                    return apiPath;
            }

            // Running efbundle from the published API folder on the server
            if (File.Exists(Path.Combine(currentDir, "appsettings.json"))
                || File.Exists(Path.Combine(currentDir, "appsettings.Production.json")))
            {
                return currentDir;
            }

            return currentDir;
        }
    }
}
