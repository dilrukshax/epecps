using Epecps.Application.DTOs.Auth;

namespace Epecps.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> SetupPasswordAsync(SetupPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequestDto request, int userId, CancellationToken cancellationToken = default);
    Task<MeDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
