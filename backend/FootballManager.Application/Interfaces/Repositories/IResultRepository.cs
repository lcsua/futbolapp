using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IResultRepository
{
    Task<Result> GetByFixtureIdAsync(Guid fixtureId, CancellationToken cancellationToken = default);
    Task AddAsync(Result result, CancellationToken cancellationToken = default);
    void Update(Result result);
    void Remove(Result result);
}
