using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Infrastructure.Persistence;
using FootballManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FootballManagerDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<ILeagueRepository, LeagueRepository>();
            services.AddScoped<ISeasonRepository, SeasonRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IUserLeagueRepository, UserLeagueRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDivisionRepository, DivisionRepository>();
            services.AddScoped<IFieldRepository, FieldRepository>();
            services.AddScoped<IDivisionSeasonRepository, DivisionSeasonRepository>();
            services.AddScoped<ITeamDivisionSeasonRepository, TeamDivisionSeasonRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
