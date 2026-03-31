namespace Epecps.Application.Interfaces;

public interface ISuperAdminBootstrapService
{
    Task EnsureSuperAdminAsync(CancellationToken cancellationToken = default);
}
