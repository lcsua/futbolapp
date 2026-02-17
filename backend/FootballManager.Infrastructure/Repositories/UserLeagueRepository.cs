using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class UserLeagueRepository : IUserLeagueRepository
    {
        private readonly FootballManagerDbContext _context;

        public UserLeagueRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> IsUserInLeagueAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.UserLeagues
                .AsNoTracking()
                .AnyAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId, cancellationToken);
        }

        public async Task AddAsync(UserLeague userLeague, CancellationToken cancellationToken = default)
        {
            await _context.UserLeagues.AddAsync(userLeague, cancellationToken);
        }
    }
}
