using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Roles.GetPermissionCatalog
{
    public record PermissionCatalogItemDto(string Code, string Name, string Module);

    public interface IGetPermissionCatalogUseCase
    {
        Task<List<PermissionCatalogItemDto>> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public class GetPermissionCatalogUseCase : IGetPermissionCatalogUseCase
    {
        private readonly IPermissionRepository _permissionRepository;

        public GetPermissionCatalogUseCase(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<List<PermissionCatalogItemDto>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
            if (permissions.Count > 0)
            {
                return permissions.Select(p => new PermissionCatalogItemDto(p.Code, p.Name, p.Module)).ToList();
            }

            return PermissionCodes.Catalog
                .Select(p => new PermissionCatalogItemDto(p.Code, p.Name, p.Module))
                .ToList();
        }
    }
}
