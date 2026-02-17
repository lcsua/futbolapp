using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class FieldBlackoutRepository : IFieldBlackoutRepository
    {
        private readonly FootballManagerDbContext _context;

        public FieldBlackoutRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<FieldBlackout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.FieldBlackouts
                .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<List<FieldBlackout>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default)
        {
            return await _context.FieldBlackouts
                .AsNoTracking()
                .Where(b => b.FieldId == fieldId)
                .OrderBy(b => b.Date).ThenBy(b => b.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(FieldBlackout blackout, CancellationToken cancellationToken = default)
        {
            await _context.FieldBlackouts.AddAsync(blackout, cancellationToken);
        }

        public void Remove(FieldBlackout blackout)
        {
            _context.FieldBlackouts.Remove(blackout);
        }
    }
}
