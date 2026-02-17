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
            // Robustly find the API folder for appsettings
            var currentDir = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(currentDir, "FootballManager.Api");
            
            // If running from Infrastructure or elsewhere, adjust
            if (!Directory.Exists(apiPath))
            {
               // Fallback strategies or assumptions
               var parent = Directory.GetParent(currentDir)?.FullName;
               if (parent != null && Directory.Exists(Path.Combine(parent, "FootballManager.Api")))
               {
                   apiPath = Path.Combine(parent, "FootballManager.Api");
               }
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var builder = new DbContextOptionsBuilder<FootballManagerDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            builder.UseNpgsql(connectionString);

            return new FootballManagerDbContext(builder.Options);
        }
    }
}
