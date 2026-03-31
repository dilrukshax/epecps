using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epecps.Infrastructure.Services;

public class SuperAdminBootstrapService : ISuperAdminBootstrapService
{
    private readonly EpecpsDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IOptions<SuperAdminSettings> _settings;
    private readonly ILogger<SuperAdminBootstrapService> _logger;

    public SuperAdminBootstrapService(
        EpecpsDbContext context,
        IPasswordService passwordService,
        IOptions<SuperAdminSettings> settings,
        ILogger<SuperAdminBootstrapService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _settings = settings;
        _logger = logger;
    }

    public async Task EnsureSuperAdminAsync(CancellationToken cancellationToken = default)
    {
        var email = _settings.Value.Email?.Trim();
        var password = _settings.Value.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Super admin bootstrap skipped because email/password are not configured.");
            return;
        }

        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);
        if (superAdminRole == null)
        {
            superAdminRole = new Role { Name = "SuperAdmin" };
            _context.Roles.Add(superAdminRole);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var defaultDepartmentId = await ResolveDepartmentIdAsync(cancellationToken);
        if (defaultDepartmentId == 0)
        {
            return;
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            user = new User
            {
                FullName = string.IsNullOrWhiteSpace(_settings.Value.FullName) ? "Super Admin" : _settings.Value.FullName.Trim(),
                Email = email,
                Status = "Active",
                DeptId = defaultDepartmentId,
                IsActive = true,
                PasswordHash = _passwordService.HashPassword(password),
                PasswordSetAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = _passwordService.HashPassword(password);
                user.PasswordSetAt = DateTime.UtcNow;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                user.Status = "Active";
            }

            if (user.DeptId <= 0)
            {
                user.DeptId = defaultDepartmentId;
            }
        }

        var hasRole = user.UserRoles.Any(ur => ur.RoleId == superAdminRole.RoleId);
        if (!hasRole)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = superAdminRole.RoleId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> ResolveDepartmentIdAsync(CancellationToken cancellationToken)
    {
        if (_settings.Value.DepartmentId.HasValue && _settings.Value.DepartmentId.Value > 0)
        {
            var configuredExists = await _context.Departments.AnyAsync(
                d => d.DeptId == _settings.Value.DepartmentId.Value,
                cancellationToken);

            if (configuredExists)
            {
                return _settings.Value.DepartmentId.Value;
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
}
