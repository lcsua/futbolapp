using FootballManager.Application.UseCases.Auth.Login;
using FootballManager.Application.UseCases.Leagues.CreateLeague;
using FootballManager.Application.UseCases.Leagues.CreateSeason;
using FootballManager.Application.UseCases.Leagues.GetLeague;
using FootballManager.Application.UseCases.Leagues.GetUserLeagues;
using FootballManager.Application.UseCases.Leagues.GetLeagueSeasons;
using FootballManager.Application.UseCases.Leagues.GetLeagueTeams;
using FootballManager.Application.UseCases.Leagues.UpdateLeague;
using FootballManager.Application.UseCases.Leagues.UpdateSeason;
using FootballManager.Application.UseCases.Leagues.GetLeagueDivisions;
using FootballManager.Application.UseCases.Leagues.CreateDivision;
using FootballManager.Application.UseCases.Leagues.UpdateDivision;
using FootballManager.Application.UseCases.Leagues.CreateTeam;
using FootballManager.Application.UseCases.Leagues.BulkCreateTeams;
using FootballManager.Application.UseCases.Leagues.UpdateTeam;
using FootballManager.Application.UseCases.Leagues.AssignDivisionToSeason;
using FootballManager.Application.UseCases.Leagues.AssignTeamToDivisionSeason;
using FootballManager.Application.UseCases.Leagues.GetLeagueFields;
using FootballManager.Application.UseCases.Leagues.CreateField;
using FootballManager.Application.UseCases.Leagues.UpdateField;
using FootballManager.Application.UseCases.Leagues.GetCompetitionRule;
using FootballManager.Application.UseCases.Leagues.UpsertCompetitionRule;
using FootballManager.Application.UseCases.Leagues.GetMatchRule;
using FootballManager.Application.UseCases.Leagues.UpsertMatchRule;
using FootballManager.Application.UseCases.Leagues.GetFieldAvailabilities;
using FootballManager.Application.UseCases.Leagues.SaveFieldAvailabilities;
using FootballManager.Application.UseCases.Leagues.GetFieldBlackouts;
using FootballManager.Application.UseCases.Leagues.CreateFieldBlackout;
using FootballManager.Application.UseCases.Leagues.DeleteFieldBlackout;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ILoginUseCase, LoginUseCase>();
            services.AddScoped<ICreateLeagueUseCase, CreateLeagueUseCase>();
            services.AddScoped<ICreateSeasonUseCase, CreateSeasonUseCase>();
            services.AddScoped<IGetLeagueUseCase, GetLeagueUseCase>();
            services.AddScoped<IGetUserLeaguesUseCase, GetUserLeaguesUseCase>();
            services.AddScoped<IGetLeagueSeasonsUseCase, GetLeagueSeasonsUseCase>();
            services.AddScoped<IGetLeagueTeamsUseCase, GetLeagueTeamsUseCase>();
            services.AddScoped<IUpdateLeagueUseCase, UpdateLeagueUseCase>();
            services.AddScoped<IUpdateSeasonUseCase, UpdateSeasonUseCase>();
            services.AddScoped<IGetLeagueDivisionsUseCase, GetLeagueDivisionsUseCase>();
            services.AddScoped<ICreateDivisionUseCase, CreateDivisionUseCase>();
            services.AddScoped<IUpdateDivisionUseCase, UpdateDivisionUseCase>();
            services.AddScoped<ICreateTeamUseCase, CreateTeamUseCase>();
            services.AddScoped<IBulkCreateTeamsUseCase, BulkCreateTeamsUseCase>();
            services.AddScoped<IUpdateTeamUseCase, UpdateTeamUseCase>();
            services.AddScoped<IAssignDivisionToSeasonUseCase, AssignDivisionToSeasonUseCase>();
            services.AddScoped<IAssignTeamToDivisionSeasonUseCase, AssignTeamToDivisionSeasonUseCase>();
            services.AddScoped<IGetLeagueFieldsUseCase, GetLeagueFieldsUseCase>();
            services.AddScoped<ICreateFieldUseCase, CreateFieldUseCase>();
            services.AddScoped<IUpdateFieldUseCase, UpdateFieldUseCase>();
            services.AddScoped<IGetCompetitionRuleUseCase, GetCompetitionRuleUseCase>();
            services.AddScoped<IUpsertCompetitionRuleUseCase, UpsertCompetitionRuleUseCase>();
            services.AddScoped<IGetMatchRuleUseCase, GetMatchRuleUseCase>();
            services.AddScoped<IUpsertMatchRuleUseCase, UpsertMatchRuleUseCase>();
            services.AddScoped<IGetFieldAvailabilitiesUseCase, GetFieldAvailabilitiesUseCase>();
            services.AddScoped<ISaveFieldAvailabilitiesUseCase, SaveFieldAvailabilitiesUseCase>();
            services.AddScoped<IGetFieldBlackoutsUseCase, GetFieldBlackoutsUseCase>();
            services.AddScoped<ICreateFieldBlackoutUseCase, CreateFieldBlackoutUseCase>();
            services.AddScoped<IDeleteFieldBlackoutUseCase, DeleteFieldBlackoutUseCase>();

            return services;
        }
    }
}
