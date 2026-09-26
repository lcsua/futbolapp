using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
    }
}
