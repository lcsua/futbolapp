using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.UpdatePlayer;

public interface IUpdatePlayerUseCase
{
    Task ExecuteAsync(UpdatePlayerRequest request, CancellationToken cancellationToken = default);
}

public sealed class UpdatePlayerRequest
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Document { get; set; }
    public string? Position { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool? IsActive { get; set; }
    public int? JerseyNumber { get; set; }
}
