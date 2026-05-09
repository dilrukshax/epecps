using Epecps.Application.DTOs.Admin;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly EpecpsDbContext _context;

    public AdminUserService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserAdminDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.ManagerMappingsAsEmployee)
                .ThenInclude(m => m.ManagerUser)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.ManagerMappingsAsEmployee)
                .ThenInclude(m => m.ManagerUser)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserAdminDto> CreateUserAsync(CreateUserAdminDto dto)
    {
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Status = dto.Status,
            IsActive = true,
            DeptId = dto.DeptId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SyncUserRoles(user.UserId, dto.Roles);
        
        if (dto.ReportingManagerId.HasValue)
        {
            await SetReportingManager(user.UserId, dto.ReportingManagerId.Value);
        }

        return await GetUserByIdAsync(user.UserId) ?? throw new Exception("Created user not found");
    }

    public async Task<UserAdminDto> UpdateUserAsync(int id, UpdateUserAdminDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) throw new KeyNotFoundException($"User with ID {id} not found");

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.Status = dto.Status;
        user.IsActive = dto.IsActive;
        user.DeptId = dto.DeptId;

        await _context.SaveChangesAsync();

        await SyncUserRoles(id, dto.Roles);
        
        if (dto.ReportingManagerId.HasValue)
        {
            await SetReportingManager(id, dto.ReportingManagerId.Value);
        }
        else
        {
            await ClearReportingManager(id);
        }

        return await GetUserByIdAsync(id) ?? throw new Exception("Updated user not found");
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) throw new KeyNotFoundException($"User with ID {id} not found");

        // Instead of hard delete, we perform a soft delete to avoid breaking historical records
        user.IsActive = false;
        user.Status = "Inactive";
        await _context.SaveChangesAsync();
    }

    private async Task SyncUserRoles(int userId, List<string> roleNames)
    {
        var userRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _context.UserRoles.RemoveRange(userRoles);
        await _context.SaveChangesAsync();

        if (roleNames != null && roleNames.Any())
        {
            var roles = await _context.Roles.Where(r => roleNames.Contains(r.Name)).ToListAsync();
            foreach (var role in roles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = role.RoleId
                });
            }
            await _context.SaveChangesAsync();
        }
    }

    private async Task SetReportingManager(int employeeId, int managerId)
    {
        var existingMapping = await _context.UserManagerMappings
            .FirstOrDefaultAsync(m => m.EmployeeUserId == employeeId);

        if (existingMapping != null)
        {
            existingMapping.ManagerUserId = managerId;
        }
        else
        {
            _context.UserManagerMappings.Add(new UserManagerMapping
            {
                EmployeeUserId = employeeId,
                ManagerUserId = managerId
            });
        }
        await _context.SaveChangesAsync();
    }

    private async Task ClearReportingManager(int employeeId)
    {
        var mappings = await _context.UserManagerMappings
            .Where(m => m.EmployeeUserId == employeeId)
            .ToListAsync();
            
        if (mappings.Any())
        {
            _context.UserManagerMappings.RemoveRange(mappings);
            await _context.SaveChangesAsync();
        }
    }

    private UserAdminDto MapToDto(User u)
    {
        var rmMapping = u.ManagerMappingsAsEmployee.FirstOrDefault();
        
        return new UserAdminDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Status = u.Status,
            IsActive = u.IsActive,
            DeptId = u.DeptId,
            DepartmentName = u.Department?.Name ?? "",
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            ReportingManagerId = rmMapping?.ManagerUserId,
            ReportingManagerName = rmMapping?.ManagerUser?.FullName
        };
    }
}
