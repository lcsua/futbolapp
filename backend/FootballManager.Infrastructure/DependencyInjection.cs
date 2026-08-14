using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.ProfessionalFootball;
using FootballManager.Application.Push;
using FootballManager.Application.Services;
using FootballManager.Infrastructure.Persistence;
using FootballManager.Infrastructure.ProfessionalFootball;
using FootballManager.Infrastructure.Push;
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

            services.Configure<WebPushOptions>(configuration.GetSection(WebPushOptions.SectionName));
            services.AddHttpClient(nameof(WebPushSender));
            services.AddSingleton<IPushDispatchQueue, InMemoryPushDispatchQueue>();
            services.AddHostedService<PushDispatchBackgroundService>();
            services.AddScoped<IWebPushSender, WebPushSender>();
            services.AddScoped<IPushFollowQuery, PushFollowQuery>();
            services.AddScoped<IPushSubscriptionService, PushSubscriptionService>();
            services.AddScoped<IPushNotificationService, PushNotificationService>();

            services.AddScoped<ILeagueRepository, LeagueRepository>();
            services.AddScoped<ISeasonRepository, SeasonRepository>();
            services.AddScoped<ITeamRepository, TeamRepository>();
            services.AddScoped<IPlayerRepository, PlayerRepository>();
            services.AddScoped<IClubRepository, ClubRepository>();
            services.AddScoped<IUserLeagueRepository, UserLeagueRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDivisionRepository, DivisionRepository>();
            services.AddScoped<IFieldRepository, FieldRepository>();
            services.AddScoped<ICompetitionRuleRepository, CompetitionRuleRepository>();
            services.AddScoped<IMatchRuleRepository, MatchRuleRepository>();
            services.AddScoped<IDivisionMatchRulesRepository, DivisionMatchRulesRepository>();
            services.AddScoped<IDivisionSeasonFieldRepository, DivisionSeasonFieldRepository>();
            services.AddScoped<IFieldAvailabilityRepository, FieldAvailabilityRepository>();
            services.AddScoped<IFieldBlackoutRepository, FieldBlackoutRepository>();
            services.AddScoped<IDivisionSeasonRepository, DivisionSeasonRepository>();
            services.AddScoped<ITeamDivisionSeasonRepository, TeamDivisionSeasonRepository>();
            services.AddScoped<IFixtureRepository, FixtureRepository>();
            services.AddScoped<IResultRepository, ResultRepository>();
            services.AddScoped<IMatchIncidentRepository, MatchIncidentRepository>();
            services.AddScoped<ILeagueDocumentCategoryRepository, LeagueDocumentCategoryRepository>();
            services.AddScoped<ILeagueDocumentRepository, LeagueDocumentRepository>();
            services.AddScoped<ITeamNameAliasRepository, TeamNameAliasRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddHttpClient<IProfessionalFootballProvider, EspnFootballProvider>(client =>
            {
                client.BaseAddress = new Uri("https://site.web.api.espn.com/");
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "User-Agent",
                    "Mozilla/5.0 (compatible; MiLiga/1.0; +https://miliga.com.ar)");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://www.espn.com.ar/");
            });

            return services;
        }
    }
}
