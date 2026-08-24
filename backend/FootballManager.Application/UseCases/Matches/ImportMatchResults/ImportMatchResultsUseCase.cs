using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.ImportMatchResults
{
    public class ImportMatchResultsUseCase : IImportMatchResultsUseCase
    {
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ILeagueRepository _leagueRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly IFixtureRepository _fixtureRepository;
        private readonly IResultRepository _resultRepository;
        private readonly ITeamNameAliasService _aliasService;
        private readonly IUnitOfWork _unitOfWork;

        public ImportMatchResultsUseCase(
            IUserLeagueRepository userLeagueRepository,
            ISeasonRepository seasonRepository,
            ILeagueRepository leagueRepository,
            IDivisionRepository divisionRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            IFixtureRepository fixtureRepository,
            IResultRepository resultRepository,
            ITeamNameAliasService aliasService,
            IUnitOfWork unitOfWork)
        {
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
            _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<ImportMatchResultsResponse> ExecuteAsync(ImportMatchResultsRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
                throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken)
                ?? throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken)
                ?? throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            SeasonGuard.EnsureOpen(season);

            var divisions = request.Divisions ?? new List<ImportMatchResultsDivisionDto>();
            if (divisions.Count == 0)
                throw new BusinessException("No divisions to import.");

            var updated = 0;
            var created = 0;
            var skipped = 0;
            var notCreated = 0;
            var warnings = new List<string>();
            var learned = new HashSet<(Guid TeamId, string Normalized)>();

            foreach (var divDto in divisions)
            {
                if (divDto.Matches == null || divDto.Matches.Count == 0)
                    continue;

                var division = await _divisionRepository.GetByIdAsync(divDto.DivisionId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Division {divDto.DivisionId} not found.");
                if (division.LeagueId != request.LeagueId)
                    throw new ForbiddenAccessException("Division does not belong to this league.");

                var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(
                    request.SeasonId, divDto.DivisionId, cancellationToken);
                if (divisionSeason == null)
                    throw new BusinessException(
                        $"Division \"{division.Name}\" is not set up for this season (no team assignments).");

                var tdsByTeamId = divisionSeason.TeamAssignments
                    .GroupBy(ta => ta.TeamId)
                    .ToDictionary(g => g.Key, g => g.First());

                var existingFixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
                    request.SeasonId, divisionSeason.Id, null, cancellationToken);

                var csvRound = divDto.Round is > 0 ? divDto.Round.Value : (int?)null;
                var scopedFixtures = csvRound.HasValue
                    ? existingFixtures.Where(f => f.RoundNumber == csvRound.Value).ToList()
                    : existingFixtures;

                var fixtureByPair = new Dictionary<(Guid Home, Guid Away), Fixture>();
                foreach (var f in scopedFixtures)
                {
                    var key = (f.HomeTeamDivisionSeasonId, f.AwayTeamDivisionSeasonId);
                    if (!fixtureByPair.ContainsKey(key))
                        fixtureByPair[key] = f;
                }

                var maxRound = existingFixtures.Count == 0 ? 0 : existingFixtures.Max(f => f.RoundNumber);
                var skippedThisDivision = 0;
                var toCreate = new List<(TeamDivisionSeason Home, TeamDivisionSeason Away, ImportMatchResultItemDto Item)>();

                foreach (var item in divDto.Matches)
                {
                    if (item.HomeTeamId == item.AwayTeamId)
                    {
                        warnings.Add($"{division.Name}: home and away team cannot be the same.");
                        continue;
                    }

                    if (!tdsByTeamId.TryGetValue(item.HomeTeamId, out var homeTds))
                    {
                        warnings.Add($"{division.Name}: home team {item.HomeTeamId} is not assigned to this division.");
                        continue;
                    }
                    if (!tdsByTeamId.TryGetValue(item.AwayTeamId, out var awayTds))
                    {
                        warnings.Add($"{division.Name}: away team {item.AwayTeamId} is not assigned to this division.");
                        continue;
                    }

                    await _aliasService.LearnAsync(league, item.HomeTeamId, item.HomeCsvName, "results-import", learned, cancellationToken);
                    await _aliasService.LearnAsync(league, item.AwayTeamId, item.AwayCsvName, "results-import", learned, cancellationToken);

                    if (fixtureByPair.TryGetValue((homeTds.Id, awayTds.Id), out var fixture))
                    {
                        if (AlreadyHasLoadedResult(fixture))
                        {
                            skipped++;
                            skippedThisDivision++;
                            continue;
                        }
                        await ApplyResultAsync(fixture, item.HomeScore, item.AwayScore, item.Status, swap: false, cancellationToken);
                        updated++;
                        continue;
                    }

                    if (fixtureByPair.TryGetValue((awayTds.Id, homeTds.Id), out var inverted))
                    {
                        if (AlreadyHasLoadedResult(inverted))
                        {
                            skipped++;
                            skippedThisDivision++;
                            continue;
                        }
                        await ApplyResultAsync(inverted, item.HomeScore, item.AwayScore, item.Status, swap: true, cancellationToken);
                        updated++;
                        continue;
                    }

                    toCreate.Add((homeTds, awayTds, item));
                }

                if (skippedThisDivision > 0)
                {
                    var roundLabel = csvRound.HasValue ? $" fecha {csvRound.Value}" : string.Empty;
                    warnings.Add(
                        $"{division.Name}{roundLabel}: {skippedThisDivision} partido(s) ya tenían resultado y no se modificaron.");
                }

                if (toCreate.Count > 0)
                {
                    // Never insert extra matches into a round that already has a fixture.
                    // Pending games in that round are updated above; only empty rounds may be created.
                    if (!CanCreateMissingPairs(csvRound, scopedFixtures.Count))
                    {
                        notCreated += toCreate.Count;
                        var examples = toCreate
                            .Take(3)
                            .Select(x =>
                            {
                                var home = string.IsNullOrWhiteSpace(x.Item.HomeCsvName) ? "local" : x.Item.HomeCsvName;
                                var away = string.IsNullOrWhiteSpace(x.Item.AwayCsvName) ? "visitante" : x.Item.AwayCsvName;
                                return $"{home} vs {away}";
                            });
                        var more = toCreate.Count > 3 ? $" y {toCreate.Count - 3} más" : string.Empty;
                        warnings.Add(
                            $"{division.Name} fecha {csvRound}: {toCreate.Count} partido(s) no coinciden con el fixture de esa fecha y no se crearon (la fecha ya tiene partidos). Revisá la columna fecha del CSV. Ej: {string.Join("; ", examples)}{more}.");
                    }
                    else
                    {
                        var newRound = csvRound ?? (maxRound + 1);
                        foreach (var (homeTds, awayTds, item) in toCreate)
                        {
                            var fixture = new Fixture(league, season, divisionSeason, homeTds, awayTds, newRound);
                            await _fixtureRepository.AddAsync(fixture, cancellationToken);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);

                            await ApplyResultAsync(fixture, item.HomeScore, item.AwayScore, item.Status, swap: false, cancellationToken);
                            created++;

                            fixtureByPair[(homeTds.Id, awayTds.Id)] = fixture;
                        }
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ImportMatchResultsResponse(updated, created, warnings, skipped, notCreated);
        }

        /// <summary>
        /// Missing CSV pairs may only create fixtures when the target round is empty (or unspecified).
        /// A round that already has matches is update-only: pending results load, existing results stay.
        /// </summary>
        internal static bool CanCreateMissingPairs(int? csvRound, int fixturesInTargetRound)
        {
            if (csvRound is > 0 && fixturesInTargetRound > 0)
                return false;
            return true;
        }

        private static bool AlreadyHasLoadedResult(Fixture fixture)
        {
            if (fixture.Result != null)
                return true;
            return fixture.Status != MatchStatus.SCHEDULED;
        }

        private async Task ApplyResultAsync(
            Fixture fixture,
            int? homeScore,
            int? awayScore,
            string? jsonStatus,
            bool swap,
            CancellationToken cancellationToken)
        {
            var hs = homeScore;
            var ascore = awayScore;
            if (swap)
                (hs, ascore) = (ascore, hs);

            var status = ResolveStatus(jsonStatus, hs, ascore);

            if (status is MatchStatus.COMPLETED or MatchStatus.PLAYED)
            {
                if (!hs.HasValue || !ascore.HasValue)
                    throw new BusinessException("Finished matches require both scores.");
                if (hs.Value < 0 || ascore.Value < 0)
                    throw new BusinessException("Scores cannot be negative.");

                var existing = await _resultRepository.GetByFixtureIdAsync(fixture.Id, cancellationToken);
                if (existing != null)
                {
                    existing.UpdateScore(hs.Value, ascore.Value);
                    _resultRepository.Update(existing);
                }
                else
                {
                    await _resultRepository.AddAsync(new Result(fixture, hs.Value, ascore.Value), cancellationToken);
                }
            }

            fixture.ChangeStatus(status);
        }

        private static MatchStatus ResolveStatus(string? jsonStatus, int? homeScore, int? awayScore)
        {
            var raw = (jsonStatus ?? string.Empty).Trim().ToLowerInvariant();
            if (raw is "suspended")
                return MatchStatus.SUSPENDED;
            if (raw is "postponed")
                return MatchStatus.POSTPONED;
            if (raw is "cancelled" or "canceled")
                return MatchStatus.CANCELLED;
            if (raw is "finished" or "completed" or "played" || (homeScore.HasValue && awayScore.HasValue))
                return MatchStatus.COMPLETED;
            return MatchStatus.SCHEDULED;
        }
    }
}
