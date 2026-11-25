using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.LeagueChampions
{
    internal sealed class ResolvedChampion
    {
        public ResolvedChampion(
            Team team,
            int year,
            string championshipName,
            string divisionName,
            Guid? seasonId,
            Guid? divisionId,
            DateOnly? wonAt,
            string? notes)
        {
            Team = team;
            Year = year;
            ChampionshipName = championshipName;
            DivisionName = divisionName;
            SeasonId = seasonId;
            DivisionId = divisionId;
            WonAt = wonAt;
            Notes = notes;
        }

        public Team Team { get; }
        public int Year { get; }
        public string ChampionshipName { get; }
        public string DivisionName { get; }
        public Guid? SeasonId { get; }
        public Guid? DivisionId { get; }
        public DateOnly? WonAt { get; }
        public string? Notes { get; }
    }

    internal static class LeagueChampionEditor
    {
        public static async Task<ResolvedChampion> ResolveAsync(
            ILeagueChampionRepository champions,
            ITeamRepository teams,
            Guid leagueId,
            LeagueChampionWriteRequest request,
            CancellationToken cancellationToken)
        {
            var existingMode = ParseMode(request.Mode);
            var team = await teams.GetByIdAsync(request.TeamId, cancellationToken);
            if (team == null)
                throw new KeyNotFoundException("No se encontró el equipo.");
            if (team.LeagueId != leagueId)
                throw new ForbiddenAccessException("El equipo no pertenece a esta liga.");

            EnsureOptionalFields(request.WonAt, request.Notes);

            if (existingMode)
                return await ResolveExistingAsync(champions, leagueId, request, team, cancellationToken);

            return await ResolveHistoricalAsync(champions, leagueId, request, team, cancellationToken);
        }

        public static async Task EnsureUniqueAsync(
            ILeagueChampionRepository champions,
            Guid leagueId,
            int year,
            string championshipName,
            string divisionName,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            var key = LeagueChampion.BuildIdentityKey(year, championshipName, divisionName);
            if (await champions.ExistsActiveIdentityAsync(leagueId, key, excludeId, cancellationToken))
                throw new BusinessException("Ya existe un campeón para esa liga, año, campeonato y división.");
        }

        private static async Task<ResolvedChampion> ResolveExistingAsync(
            ILeagueChampionRepository champions,
            Guid leagueId,
            LeagueChampionWriteRequest request,
            Team team,
            CancellationToken cancellationToken)
        {
            if (!request.SeasonId.HasValue || !request.DivisionId.HasValue)
                throw new BusinessException("Elegí el campeonato y la división existentes.");

            var link = await champions.GetDivisionSeasonLinkAsync(
                leagueId,
                request.SeasonId.Value,
                request.DivisionId.Value,
                cancellationToken);

            if (link == null)
                throw new BusinessException("El campeonato y la división tienen que existir en esta liga y estar asociados.");

            if (!Contains(link.TeamIds, team.Id))
            {
                throw new BusinessException(
                    "El equipo elegido no participa de esa división. El campeón se confirma manualmente y no se toma de la tabla de posiciones.");
            }

            if (link.Year < LeagueChampion.MinYear || link.Year > LeagueChampion.MaxYear)
                throw new BusinessException($"El año del campeonato tiene que estar entre {LeagueChampion.MinYear} y {LeagueChampion.MaxYear}.");

            return new ResolvedChampion(
                team,
                link.Year,
                link.SeasonName,
                link.DivisionName,
                request.SeasonId,
                request.DivisionId,
                request.WonAt,
                request.Notes);
        }

        private static async Task<ResolvedChampion> ResolveHistoricalAsync(
            ILeagueChampionRepository champions,
            Guid leagueId,
            LeagueChampionWriteRequest request,
            Team team,
            CancellationToken cancellationToken)
        {
            if (!request.Year.HasValue)
                throw new BusinessException("El año es obligatorio.");
            if (request.Year.Value < LeagueChampion.MinYear || request.Year.Value > LeagueChampion.MaxYear)
                throw new BusinessException($"El año tiene que estar entre {LeagueChampion.MinYear} y {LeagueChampion.MaxYear}.");
            if (string.IsNullOrWhiteSpace(request.ChampionshipName))
                throw new BusinessException("El nombre del campeonato es obligatorio.");
            if (string.IsNullOrWhiteSpace(request.DivisionName))
                throw new BusinessException("El nombre de la división es obligatorio.");

            var championship = LeagueChampion.NormalizeLabel(request.ChampionshipName);
            var division = LeagueChampion.NormalizeLabel(request.DivisionName);
            if (championship.Length > LeagueChampion.MaxChampionshipNameLength)
                throw new BusinessException($"El nombre del campeonato no puede superar {LeagueChampion.MaxChampionshipNameLength} caracteres.");
            if (division.Length > LeagueChampion.MaxDivisionNameLength)
                throw new BusinessException($"El nombre de la división no puede superar {LeagueChampion.MaxDivisionNameLength} caracteres.");

            Guid? seasonId = null;
            Guid? divisionId = null;

            if (request.SeasonId.HasValue && request.DivisionId.HasValue)
            {
                var link = await champions.GetDivisionSeasonLinkAsync(
                    leagueId,
                    request.SeasonId.Value,
                    request.DivisionId.Value,
                    cancellationToken);
                if (link == null)
                    throw new BusinessException("No se puede vincular: esa división no forma parte del campeonato en esta liga.");

                seasonId = request.SeasonId;
                divisionId = request.DivisionId;
            }
            else if (request.DivisionId.HasValue)
            {
                var belongs = await champions.DivisionBelongsToLeagueAsync(leagueId, request.DivisionId.Value, cancellationToken);
                if (!belongs)
                    throw new ForbiddenAccessException("La división no pertenece a esta liga.");
                divisionId = request.DivisionId;
            }
            else if (request.SeasonId.HasValue)
            {
                throw new BusinessException("Para vincular el título indicá campeonato y división.");
            }

            return new ResolvedChampion(
                team,
                request.Year.Value,
                championship,
                division,
                seasonId,
                divisionId,
                request.WonAt,
                request.Notes);
        }

        private static bool ParseMode(string? mode)
        {
            if (string.Equals(mode?.Trim(), "existing", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mode?.Trim(), "historical", StringComparison.OrdinalIgnoreCase))
                return false;
            throw new BusinessException("La modalidad tiene que ser existing o historical.");
        }

        private static void EnsureOptionalFields(DateOnly? wonAt, string? notes)
        {
            if (wonAt.HasValue && (wonAt.Value.Year < LeagueChampion.MinYear || wonAt.Value.Year > LeagueChampion.MaxYear))
                throw new BusinessException($"La fecha de consagración tiene que estar entre {LeagueChampion.MinYear} y {LeagueChampion.MaxYear}.");

            if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > LeagueChampion.MaxNotesLength)
                throw new BusinessException($"Las observaciones no pueden superar {LeagueChampion.MaxNotesLength} caracteres.");
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<Guid> ids, Guid teamId)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == teamId)
                    return true;
            }

            return false;
        }
    }
}
