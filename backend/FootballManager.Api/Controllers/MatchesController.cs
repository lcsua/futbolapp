using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Matches.GetMatches;
using FootballManager.Application.UseCases.Matches.GetMatchById;
using FootballManager.Application.UseCases.Matches.UpdateMatchResult;
using FootballManager.Application.UseCases.Matches.ImportMatchResults;
using FootballManager.Application.UseCases.Matches.ClearRoundResults;
using FootballManager.Application.UseCases.Matches.SwapDivisionHomeAway;
using FootballManager.Application.UseCases.Matches.AddMatchIncident;
using FootballManager.Application.UseCases.Matches.DeleteMatchIncident;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/leagues/{leagueId}/matches")]
    public class MatchesController : ControllerBase
    {
        private readonly IGetMatchesUseCase _getMatchesUseCase;
        private readonly IGetMatchByIdUseCase _getMatchByIdUseCase;
        private readonly IUpdateMatchResultUseCase _updateMatchResultUseCase;
        private readonly IImportMatchResultsUseCase _importMatchResultsUseCase;
        private readonly IClearRoundResultsUseCase _clearRoundResultsUseCase;
        private readonly ISwapDivisionHomeAwayUseCase _swapDivisionHomeAwayUseCase;
        private readonly IAddMatchIncidentUseCase _addMatchIncidentUseCase;
        private readonly IDeleteMatchIncidentUseCase _deleteMatchIncidentUseCase;

        public MatchesController(
            IGetMatchesUseCase getMatchesUseCase,
            IGetMatchByIdUseCase getMatchByIdUseCase,
            IUpdateMatchResultUseCase updateMatchResultUseCase,
            IImportMatchResultsUseCase importMatchResultsUseCase,
            IClearRoundResultsUseCase clearRoundResultsUseCase,
            ISwapDivisionHomeAwayUseCase swapDivisionHomeAwayUseCase,
            IAddMatchIncidentUseCase addMatchIncidentUseCase,
            IDeleteMatchIncidentUseCase deleteMatchIncidentUseCase)
        {
            _getMatchesUseCase = getMatchesUseCase ?? throw new ArgumentNullException(nameof(getMatchesUseCase));
            _getMatchByIdUseCase = getMatchByIdUseCase ?? throw new ArgumentNullException(nameof(getMatchByIdUseCase));
            _updateMatchResultUseCase = updateMatchResultUseCase ?? throw new ArgumentNullException(nameof(updateMatchResultUseCase));
            _importMatchResultsUseCase = importMatchResultsUseCase ?? throw new ArgumentNullException(nameof(importMatchResultsUseCase));
            _clearRoundResultsUseCase = clearRoundResultsUseCase ?? throw new ArgumentNullException(nameof(clearRoundResultsUseCase));
            _swapDivisionHomeAwayUseCase = swapDivisionHomeAwayUseCase ?? throw new ArgumentNullException(nameof(swapDivisionHomeAwayUseCase));
            _addMatchIncidentUseCase = addMatchIncidentUseCase ?? throw new ArgumentNullException(nameof(addMatchIncidentUseCase));
            _deleteMatchIncidentUseCase = deleteMatchIncidentUseCase ?? throw new ArgumentNullException(nameof(deleteMatchIncidentUseCase));
        }

        [HttpGet]
        public async Task<IActionResult> GetMatches(
            [FromRoute] Guid leagueId,
            [FromQuery] Guid seasonId,
            [FromQuery] Guid? divisionId,
            [FromQuery] int? round,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetMatchesRequest
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                DivisionId = divisionId,
                Round = round,
                UserId = userId
            };
            var response = await _getMatchesUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("import-results")]
        public async Task<IActionResult> ImportMatchResults(
            [FromRoute] Guid leagueId,
            [FromBody] ImportMatchResultsRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;
            var response = await _importMatchResultsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{matchId}")]
        public async Task<IActionResult> GetMatchById(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid matchId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetMatchByIdRequest { LeagueId = leagueId, MatchId = matchId, UserId = userId };
            var response = await _getMatchByIdUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{matchId}/result")]
        public async Task<IActionResult> UpdateMatchResult(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid matchId,
            [FromBody] UpdateMatchResultRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _updateMatchResultUseCase.ExecuteAsync(leagueId, matchId, userId, request, cancellationToken);
            return NoContent();
        }

        [HttpPost("clear-round-results")]
        public async Task<IActionResult> ClearRoundResults(
            [FromRoute] Guid leagueId,
            [FromBody] ClearRoundResultsBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _clearRoundResultsUseCase.ExecuteAsync(new ClearRoundResultsRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                SeasonId = body.SeasonId,
                DivisionId = body.DivisionId,
                Round = body.Round,
            }, cancellationToken);

            return Ok(response);
        }

        [HttpPost("swap-home-away")]
        public async Task<IActionResult> SwapHomeAway(
            [FromRoute] Guid leagueId,
            [FromBody] SwapHomeAwayBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _swapDivisionHomeAwayUseCase.ExecuteAsync(new SwapDivisionHomeAwayRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                SeasonId = body.SeasonId,
                DivisionId = body.DivisionId,
            }, cancellationToken);

            return Ok(response);
        }

        [HttpPost("{matchId}/incidents")]
        public async Task<IActionResult> AddMatchIncident(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid matchId,
            [FromBody] AddMatchIncidentRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _addMatchIncidentUseCase.ExecuteAsync(leagueId, matchId, userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetMatchById), new { leagueId, matchId }, new { id = response.Id });
        }

        [HttpDelete("incidents/{incidentId}")]
        public async Task<IActionResult> DeleteMatchIncident(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid incidentId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _deleteMatchIncidentUseCase.ExecuteAsync(leagueId, incidentId, userId, cancellationToken);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Guid.Empty;
            return userId;
        }
    }

    public class ClearRoundResultsBody
    {
        public Guid SeasonId { get; set; }
        public Guid DivisionId { get; set; }
        public int Round { get; set; }
    }

    public class SwapHomeAwayBody
    {
        public Guid SeasonId { get; set; }
        public Guid DivisionId { get; set; }
    }
}
