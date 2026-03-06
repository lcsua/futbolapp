using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class ResultRepository : IResultRepository
    {
        private readonly FootballManagerDbContext _context;

        public ResultRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result> GetByFixtureIdAsync(Guid fixtureId, CancellationToken cancellationToken = default)
        {
            return await _context.Results
                .FirstOrDefaultAsync(r => r.FixtureId == fixtureId, cancellationToken);
        }

        public async Task AddAsync(Result result, CancellationToken cancellationToken = default)
        {
            await _context.Results.AddAsync(result, cancellationToken);
        }

        public void Update(Result result)
        {
            _context.Results.Update(result);
        }
    }
}
