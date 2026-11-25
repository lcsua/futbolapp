using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.LeagueChampions
{
    public sealed class LeagueChampionListFilter
    {
        public int? Year { get; init; }
        public string? Championship { get; init; }
        public string? Division { get; init; }
        public Guid? TeamId { get; init; }
    }

    public sealed class DivisionSeasonLink
    {
        public DivisionSeasonLink(
            Guid divisionSeasonId,
            string seasonName,
            int year,
            string divisionName,
            IReadOnlyList<Guid> teamIds)
        {
            DivisionSeasonId = divisionSeasonId;
            SeasonName = seasonName;
            Year = year;
            DivisionName = divisionName;
            TeamIds = teamIds;
        }

        public Guid DivisionSeasonId { get; }
        public string SeasonName { get; }
        public int Year { get; }
        public string DivisionName { get; }
        public IReadOnlyList<Guid> TeamIds { get; }
    }

    public sealed record LeagueChampionSeasonOptionDto(Guid Id, string Name, int Year, bool IsActive);

    public sealed record LeagueChampionDivisionOptionDto(Guid Id, string Name);

    public sealed record LeagueChampionTeamOptionDto(
        Guid Id,
        string Name,
        string? LogoUrl,
        bool IsArchived,
        bool IsInactive);

    public sealed record LeagueChampionRosterDto(Guid SeasonId, Guid DivisionId, IReadOnlyList<Guid> TeamIds);

    public sealed class LeagueChampionCatalog
    {
        public LeagueChampionCatalog(
            IReadOnlyList<LeagueChampionSeasonOptionDto> seasons,
            IReadOnlyList<LeagueChampionDivisionOptionDto> divisions,
            IReadOnlyList<LeagueChampionTeamOptionDto> teams,
            IReadOnlyList<LeagueChampionRosterDto> rosters)
        {
            Seasons = seasons;
            Divisions = divisions;
            Teams = teams;
            Rosters = rosters;
        }

        public IReadOnlyList<LeagueChampionSeasonOptionDto> Seasons { get; }
        public IReadOnlyList<LeagueChampionDivisionOptionDto> Divisions { get; }
        public IReadOnlyList<LeagueChampionTeamOptionDto> Teams { get; }
        public IReadOnlyList<LeagueChampionRosterDto> Rosters { get; }
    }

    public sealed class LeagueChampionPhotoDto
    {
        public Guid Id { get; init; }
        public string ImageUrl { get; init; } = string.Empty;
        public string? ThumbUrl { get; init; }
        public string? Caption { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsCover { get; init; }
    }

    public class LeagueChampionListItemDto
    {
        public Guid Id { get; init; }
        public Guid TeamId { get; init; }
        public string TeamName { get; init; } = string.Empty;
        public string? TeamLogoUrl { get; init; }
        public bool TeamIsArchived { get; init; }
        public int Year { get; init; }
        public string ChampionshipName { get; init; } = string.Empty;
        public string DivisionName { get; init; } = string.Empty;
        public Guid? SeasonId { get; init; }
        public Guid? DivisionId { get; init; }
        public DateOnly? WonAt { get; init; }
        public int PhotoCount { get; init; }
        public string? CoverImageUrl { get; init; }
        public string? CoverThumbUrl { get; init; }
    }

    public sealed class LeagueChampionDetailDto : LeagueChampionListItemDto
    {
        public string? Notes { get; init; }
        public IReadOnlyList<LeagueChampionPhotoDto> Photos { get; init; } = Array.Empty<LeagueChampionPhotoDto>();
    }

    public sealed class LeagueChampionWriteRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Mode { get; set; } = string.Empty;
        public Guid TeamId { get; set; }
        public Guid? SeasonId { get; set; }
        public Guid? DivisionId { get; set; }
        public int? Year { get; set; }
        public string? ChampionshipName { get; set; }
        public string? DivisionName { get; set; }
        public DateOnly? WonAt { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class LeagueChampionPhotoWrite
    {
        public Guid Id { get; set; }
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsCover { get; set; }
    }

    public static class LeagueChampionCatalogMapper
    {
        public static IReadOnlyList<LeagueChampionTeamOptionDto> WithActivity(
            IReadOnlyList<LeagueChampionTeamOptionDto> teams,
            IReadOnlyList<LeagueChampionSeasonOptionDto> seasons,
            IReadOnlyList<LeagueChampionRosterDto> rosters)
        {
            var activeSeasonIds = new HashSet<Guid>();
            foreach (var season in seasons)
            {
                if (season.IsActive)
                    activeSeasonIds.Add(season.Id);
            }

            var activeTeamIds = new HashSet<Guid>();
            foreach (var roster in rosters)
            {
                if (!activeSeasonIds.Contains(roster.SeasonId))
                    continue;
                foreach (var teamId in roster.TeamIds)
                    activeTeamIds.Add(teamId);
            }

            var result = new List<LeagueChampionTeamOptionDto>(teams.Count);
            foreach (var team in teams)
            {
                result.Add(team with
                {
                    IsInactive = team.IsArchived || !activeTeamIds.Contains(team.Id)
                });
            }

            return result;
        }
    }
}
