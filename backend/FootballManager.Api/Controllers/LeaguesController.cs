using System;
using System.Threading;
using System.Threading.Tasks;
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
using FootballManager.Application.UseCases.Leagues.UpdateTeam;
using FootballManager.Application.UseCases.Leagues.AssignDivisionToSeason;
using FootballManager.Application.UseCases.Leagues.AssignTeamToDivisionSeason;
using FootballManager.Application.UseCases.Leagues.GetLeagueFields;
using FootballManager.Application.UseCases.Leagues.CreateField;
using FootballManager.Application.UseCases.Leagues.UpdateField;
using FootballManager.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaguesController : ControllerBase
    {
        private readonly ICreateLeagueUseCase _createLeagueUseCase;
        private readonly ICreateSeasonUseCase _createSeasonUseCase;
        private readonly IGetLeagueUseCase _getLeagueUseCase;
        private readonly IGetUserLeaguesUseCase _getUserLeaguesUseCase;
        private readonly IGetLeagueSeasonsUseCase _getLeagueSeasonsUseCase;
        private readonly IGetLeagueTeamsUseCase _getLeagueTeamsUseCase;
        private readonly IUpdateLeagueUseCase _updateLeagueUseCase;
        private readonly IUpdateSeasonUseCase _updateSeasonUseCase;
        private readonly IGetLeagueDivisionsUseCase _getLeagueDivisionsUseCase;
        private readonly ICreateDivisionUseCase _createDivisionUseCase;
        private readonly IUpdateDivisionUseCase _updateDivisionUseCase;
        private readonly ICreateTeamUseCase _createTeamUseCase;
        private readonly IUpdateTeamUseCase _updateTeamUseCase;
        private readonly IAssignDivisionToSeasonUseCase _assignDivisionToSeasonUseCase;
        private readonly IAssignTeamToDivisionSeasonUseCase _assignTeamToDivisionSeasonUseCase;
        private readonly IGetLeagueFieldsUseCase _getLeagueFieldsUseCase;
        private readonly ICreateFieldUseCase _createFieldUseCase;
        private readonly IUpdateFieldUseCase _updateFieldUseCase;

        public LeaguesController(
            ICreateLeagueUseCase createLeagueUseCase,
            ICreateSeasonUseCase createSeasonUseCase,
            IGetLeagueUseCase getLeagueUseCase,
            IGetUserLeaguesUseCase getUserLeaguesUseCase,
            IGetLeagueSeasonsUseCase getLeagueSeasonsUseCase,
            IGetLeagueTeamsUseCase getLeagueTeamsUseCase,
            IUpdateLeagueUseCase updateLeagueUseCase,
            IUpdateSeasonUseCase updateSeasonUseCase,
            IGetLeagueDivisionsUseCase getLeagueDivisionsUseCase,
            ICreateDivisionUseCase createDivisionUseCase,
            IUpdateDivisionUseCase updateDivisionUseCase,
            ICreateTeamUseCase createTeamUseCase,
            IUpdateTeamUseCase updateTeamUseCase,
            IAssignDivisionToSeasonUseCase assignDivisionToSeasonUseCase,
            IAssignTeamToDivisionSeasonUseCase assignTeamToDivisionSeasonUseCase,
            IGetLeagueFieldsUseCase getLeagueFieldsUseCase,
            ICreateFieldUseCase createFieldUseCase,
            IUpdateFieldUseCase updateFieldUseCase)
        {
            _createLeagueUseCase = createLeagueUseCase ?? throw new ArgumentNullException(nameof(createLeagueUseCase));
            _createSeasonUseCase = createSeasonUseCase ?? throw new ArgumentNullException(nameof(createSeasonUseCase));
            _getLeagueUseCase = getLeagueUseCase ?? throw new ArgumentNullException(nameof(getLeagueUseCase));
            _getUserLeaguesUseCase = getUserLeaguesUseCase ?? throw new ArgumentNullException(nameof(getUserLeaguesUseCase));
            _getLeagueSeasonsUseCase = getLeagueSeasonsUseCase ?? throw new ArgumentNullException(nameof(getLeagueSeasonsUseCase));
            _getLeagueTeamsUseCase = getLeagueTeamsUseCase ?? throw new ArgumentNullException(nameof(getLeagueTeamsUseCase));
            _updateLeagueUseCase = updateLeagueUseCase ?? throw new ArgumentNullException(nameof(updateLeagueUseCase));
            _updateSeasonUseCase = updateSeasonUseCase ?? throw new ArgumentNullException(nameof(updateSeasonUseCase));
            _getLeagueDivisionsUseCase = getLeagueDivisionsUseCase ?? throw new ArgumentNullException(nameof(getLeagueDivisionsUseCase));
            _createDivisionUseCase = createDivisionUseCase ?? throw new ArgumentNullException(nameof(createDivisionUseCase));
            _updateDivisionUseCase = updateDivisionUseCase ?? throw new ArgumentNullException(nameof(updateDivisionUseCase));
            _createTeamUseCase = createTeamUseCase ?? throw new ArgumentNullException(nameof(createTeamUseCase));
            _updateTeamUseCase = updateTeamUseCase ?? throw new ArgumentNullException(nameof(updateTeamUseCase));
            _assignDivisionToSeasonUseCase = assignDivisionToSeasonUseCase ?? throw new ArgumentNullException(nameof(assignDivisionToSeasonUseCase));
            _assignTeamToDivisionSeasonUseCase = assignTeamToDivisionSeasonUseCase ?? throw new ArgumentNullException(nameof(assignTeamToDivisionSeasonUseCase));
            _getLeagueFieldsUseCase = getLeagueFieldsUseCase ?? throw new ArgumentNullException(nameof(getLeagueFieldsUseCase));
            _createFieldUseCase = createFieldUseCase ?? throw new ArgumentNullException(nameof(createFieldUseCase));
            _updateFieldUseCase = updateFieldUseCase ?? throw new ArgumentNullException(nameof(updateFieldUseCase));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLeagueRequest request, CancellationToken cancellationToken)
        {
            // Extract UserId from claims
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            request.UserId = userId;

            var response = await _createLeagueUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { leagueId = response.Id }, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyLeagues(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetUserLeaguesRequest(userId);
            var response = await _getUserLeaguesUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response.Leagues);
        }

        [HttpGet("{leagueId}")]
        public async Task<IActionResult> GetById([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetLeagueRequest(leagueId, userId);
            var response = await _getLeagueUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpPut("{leagueId}")]
        public async Task<IActionResult> Update([FromRoute] Guid leagueId, [FromBody] UpdateLeagueRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;
            await _updateLeagueUseCase.ExecuteAsync(request, cancellationToken);
            return NoContent();
        }

        [HttpGet("{leagueId}/seasons")]
        public async Task<IActionResult> GetSeasons([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetLeagueSeasonsRequest(leagueId, userId);
            var response = await _getLeagueSeasonsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response.Seasons);
        }

        [HttpPost("{leagueId}/seasons")]
        public async Task<IActionResult> CreateSeason([FromRoute] Guid leagueId, [FromBody] CreateSeasonRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;

            var response = await _createSeasonUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSeasons), new { leagueId }, response);
        }

        [HttpPut("{leagueId}/seasons/{seasonId}")]
        public async Task<IActionResult> UpdateSeason([FromRoute] Guid leagueId, [FromRoute] Guid seasonId, [FromBody] UpdateSeasonRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.SeasonId = seasonId;
            request.UserId = userId;
            await _updateSeasonUseCase.ExecuteAsync(request, cancellationToken);
            return NoContent();
        }

        [HttpGet("{leagueId}/divisions")]
        public async Task<IActionResult> GetDivisions([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetLeagueDivisionsRequest(leagueId, userId);
            var response = await _getLeagueDivisionsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response.Divisions);
        }

        [HttpPost("{leagueId}/divisions")]
        public async Task<IActionResult> CreateDivision([FromRoute] Guid leagueId, [FromBody] CreateDivisionRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;
            var response = await _createDivisionUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetDivisions), new { leagueId }, response);
        }

        [HttpPut("{leagueId}/divisions/{divisionId}")]
        public async Task<IActionResult> UpdateDivision([FromRoute] Guid leagueId, [FromRoute] Guid divisionId, [FromBody] UpdateDivisionRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.DivisionId = divisionId;
            request.UserId = userId;
            await _updateDivisionUseCase.ExecuteAsync(request, cancellationToken);
            return NoContent();
        }

        [HttpGet("{leagueId}/teams")]
        public async Task<IActionResult> GetTeams([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetLeagueTeamsRequest(leagueId, userId);
            var response = await _getLeagueTeamsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response.Teams);
        }

        [HttpPost("{leagueId}/teams")]
        public async Task<IActionResult> CreateTeam([FromRoute] Guid leagueId, [FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;
            var response = await _createTeamUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetTeams), new { leagueId }, response);
        }

        [HttpPut("{leagueId}/teams/{teamId}")]
        public async Task<IActionResult> UpdateTeam([FromRoute] Guid leagueId, [FromRoute] Guid teamId, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.TeamId = teamId;
            request.UserId = userId;
            await _updateTeamUseCase.ExecuteAsync(request, cancellationToken);
            return NoContent();
        }

        [HttpPost("{leagueId}/seasons/{seasonId}/divisions")]
        public async Task<IActionResult> AssignDivisionToSeason([FromRoute] Guid leagueId, [FromRoute] Guid seasonId, [FromBody] AssignDivisionToSeasonRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.SeasonId = seasonId;
            request.UserId = userId;
            var response = await _assignDivisionToSeasonUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSeasons), new { leagueId }, response);
        }

        [HttpPost("{leagueId}/seasons/{seasonId}/divisions/{divisionId}/teams")]
        public async Task<IActionResult> AssignTeamToDivisionSeason([FromRoute] Guid leagueId, [FromRoute] Guid seasonId, [FromRoute] Guid divisionId, [FromBody] AssignTeamToDivisionSeasonRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.SeasonId = seasonId;
            request.DivisionId = divisionId;
            request.UserId = userId;
            var response = await _assignTeamToDivisionSeasonUseCase.ExecuteAsync(request, cancellationToken);
            return Created(string.Empty, response);
        }

        [HttpGet("{leagueId}/fields")]
        public async Task<IActionResult> GetFields([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new GetLeagueFieldsRequest(leagueId, userId);
            var response = await _getLeagueFieldsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response.Fields);
        }

        [HttpPost("{leagueId}/fields")]
        public async Task<IActionResult> CreateField([FromRoute] Guid leagueId, [FromBody] CreateFieldRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.UserId = userId;
            var response = await _createFieldUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetFields), new { leagueId }, response);
        }

        [HttpPut("{leagueId}/fields/{fieldId}")]
        public async Task<IActionResult> UpdateField([FromRoute] Guid leagueId, [FromRoute] Guid fieldId, [FromBody] UpdateFieldRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            request.LeagueId = leagueId;
            request.FieldId = fieldId;
            request.UserId = userId;
            await _updateFieldUseCase.ExecuteAsync(request, cancellationToken);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Guid.Empty;
            }
            return userId;
        }
    }
}
