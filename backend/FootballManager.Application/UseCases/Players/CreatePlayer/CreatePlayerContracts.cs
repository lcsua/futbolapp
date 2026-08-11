using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.CreatePlayer;

public interface ICreatePlayerUseCase
{
    Task<CreatePlayerResponse> ExecuteAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default);
}

public sealed class CreatePlayerRequest
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Document { get; set; }
    public string? Position { get; set; }
    public DateOnly? BirthDate { get; set; }
}

public sealed class CreatePlayerResponse
{
    public Guid Id { get; }
    public CreatePlayerResponse(Guid id) => Id = id;
}
