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
    public class FieldAvailabilityRepository : IFieldAvailabilityRepository
    {
        private readonly FootballManagerDbContext _context;

        public FieldAvailabilityRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<FieldAvailability>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default)
        {
            return await _context.FieldAvailabilities
                .AsNoTracking()
                .Where(a => a.FieldId == fieldId)
                .OrderBy(a => a.DayOfWeek)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(FieldAvailability availability, CancellationToken cancellationToken = default)
        {
            await _context.FieldAvailabilities.AddAsync(availability, cancellationToken);
        }

        public void Update(FieldAvailability availability)
        {
            _context.FieldAvailabilities.Update(availability);
        }

        public void Remove(FieldAvailability availability)
        {
            _context.FieldAvailabilities.Remove(availability);
        }

        public async Task RemoveAllByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default)
        {
            var list = await _context.FieldAvailabilities.Where(a => a.FieldId == fieldId).ToListAsync(cancellationToken);
            _context.FieldAvailabilities.RemoveRange(list);
        }
    }
}
