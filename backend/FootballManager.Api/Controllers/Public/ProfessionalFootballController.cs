using FootballManager.Application.ProfessionalFootball;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[AllowAnonymous]
[Route("api/public/professional-football")]
public sealed class ProfessionalFootballController : ControllerBase
{
    private readonly IProfessionalFootballAppService _service;
    private readonly ILogger<ProfessionalFootballController> _logger;

    public ProfessionalFootballController(
        IProfessionalFootballAppService service,
        ILogger<ProfessionalFootballController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("competitions")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _service.GetCompetitionsAsync(cancellationToken);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list professional competitions");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "No pudimos obtener la información del torneo en este momento. Intentá nuevamente más tarde."
            });
        }
    }

    [HttpGet("competitions/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _service.GetCompetitionDetailAsync(slug, cancellationToken);
            if (detail != null)
                return Ok(detail);

            if (ProfessionalCompetitionsCatalog.GetBySlug(slug) != null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "No pudimos obtener la información del torneo en este momento. Intentá nuevamente más tarde."
                });
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load professional competition {Slug}", slug);
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "No pudimos obtener la información del torneo en este momento. Intentá nuevamente más tarde."
            });
        }
    }
}
