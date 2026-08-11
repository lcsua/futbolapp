using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.ImportPlayers;

public interface IImportPlayersUseCase
{
    Task<ImportPlayersResponse> ExecuteAsync(ImportPlayersRequest request, CancellationToken cancellationToken = default);
}

public sealed class ImportPlayersRequest
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public List<ImportPlayerItem> Players { get; set; } = new();
}

public sealed class ImportPlayerItem
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Document { get; set; }
    public string? Position { get; set; }
    public DateOnly? BirthDate { get; set; }
}

public sealed class ImportPlayersResponse
{
    public int CreatedCount { get; }
    public IReadOnlyList<Guid> PlayerIds { get; }

    public ImportPlayersResponse(int createdCount, IReadOnlyList<Guid> playerIds)
    {
        CreatedCount = createdCount;
        PlayerIds = playerIds;
    }
}
