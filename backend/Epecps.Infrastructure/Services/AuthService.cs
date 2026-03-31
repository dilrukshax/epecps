using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Epecps.Application.DTOs.Auth;
using Epecps.Application.Exceptions;
using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Epecps.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly EpecpsDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        EpecpsDbContext context,
        IPasswordService passwordService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidateEmail(email);

        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ConflictException("PASSWORD_SETUP_REQUIRED", "Password setup is required for this account.");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginCount = 0;
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> SetupPasswordAsync(SetupPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidateEmail(email);
        EnsurePasswordMatch(request.Password, request.ConfirmPassword);
        ValidatePasswordStrength(request.Password);

        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new NotFoundException("User account was not found.");
        }

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ConflictException("PASSWORD_ALREADY_SET", "Password was already created for this account.");
        }

        user.PasswordHash = _passwordService.HashPassword(request.Password);
        user.PasswordSetAt = DateTime.UtcNow;
        user.LastLoginAt = DateTime.UtcNow;
        user.Status = string.IsNullOrWhiteSpace(user.Status) ? "Active" : user.Status;
        user.IsActive = true;

        // Keep imported role configuration, but ensure baseline employee access exists.
        await EnsureRoleAsync(user, "Employee", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var hydratedUser = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.UserId == user.UserId, cancellationToken);

        return await CreateAuthResponseAsync(hydratedUser, cancellationToken);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        ValidateEmail(email);
        EnsurePasswordMatch(request.Password, request.ConfirmPassword);
        ValidatePasswordStrength(request.Password);

        var exists = await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "An account with this email already exists.");
        }

        var departmentId = await ResolveDepartmentIdAsync(request.DepartmentId, cancellationToken);
        var user = new User
        {
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? email.Split('@')[0] : request.FullName.Trim(),
            Email = email,
            Status = "Active",
            DeptId = departmentId,
            PasswordHash = _passwordService.HashPassword(request.Password),
            PasswordSetAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        await EnsureRoleAsync(user, "Employee", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var hydratedUser = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.UserId == user.UserId, cancellationToken);

        return await CreateAuthResponseAsync(hydratedUser, cancellationToken);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Department)
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken == null || storedToken.RevokedAt.HasValue || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = storedToken.User;
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        var newRawRefreshToken = GenerateRawToken();
        var newRefreshHash = HashToken(newRawRefreshToken);
        var newRefreshExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshHash;
        storedToken.ReasonRevoked = "Rotated";

        var newRefreshToken = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = newRefreshHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = newRefreshExpiry
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var accessExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);
        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var accessToken = GenerateAccessToken(user, roles, accessExpiry);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiry,
            RefreshToken = newRawRefreshToken,
            RefreshTokenExpiresAtUtc = newRefreshExpiry,
            User = MapUser(user, roles)
        };
    }

    public async Task LogoutAsync(RefreshTokenRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.UserId == userId, cancellationToken);

        if (refreshToken == null || refreshToken.RevokedAt.HasValue)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReasonRevoked = "Logout";
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeDto> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        return MapUser(user, roles);
    }

    private async Task<AuthResponseDto> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList();
        var accessExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);
        var accessToken = GenerateAccessToken(user, roles, accessExpiry);

        var rawRefreshToken = GenerateRawToken();
        var refreshTokenHash = HashToken(rawRefreshToken);
        var refreshExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiry
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExpiry,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiry,
            User = MapUser(user, roles)
        };
    }

    private string GenerateAccessToken(User user, IReadOnlyCollection<string> roles, DateTime expiresAtUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("email", user.Email),
            new("preferred_username", user.Email),
            new("userId", user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidationException("A valid email is required.");
        }
    }

    private static void EnsurePasswordMatch(string password, string confirmPassword)
    {
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            throw new ValidationException("Password and confirm password must match.");
        }
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters long.");
        }
    }

    private async Task<int> ResolveDepartmentIdAsync(int? requestedDepartmentId, CancellationToken cancellationToken)
    {
        if (requestedDepartmentId.HasValue && requestedDepartmentId.Value > 0)
        {
            var requestedExists = await _context.Departments.AnyAsync(
                d => d.DeptId == requestedDepartmentId.Value,
                cancellationToken);

            if (requestedExists)
            {
                return requestedDepartmentId.Value;
            }
        }

        var firstDepartment = await _context.Departments
            .OrderBy(d => d.DeptId)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstDepartment != null)
        {
            return firstDepartment.DeptId;
        }

        var department = new Department { Name = "General" };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);
        return department.DeptId;
    }

    private async Task EnsureRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role == null)
        {
            role = new Role { Name = roleName };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var hasRole = await _context.UserRoles.AnyAsync(
            ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId,
            cancellationToken);

        if (!hasRole)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = role.RoleId
            });
        }
    }

    private static MeDto MapUser(User user, IReadOnlyCollection<string> roles)
    {
        return new MeDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Status = user.Status,
            IsActive = user.IsActive,
            DepartmentId = user.DeptId,
            DepartmentName = user.Department?.Name ?? string.Empty,
            Roles = roles.ToList()
        };
    }
}
