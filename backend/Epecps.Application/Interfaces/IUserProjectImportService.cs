using Epecps.Application.DTOs.AdminImport;

namespace Epecps.Application.Interfaces;

public interface IUserProjectImportService
{
    byte[] GenerateTemplate();
    Task<UsersProjectsImportResultDto> ImportAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
