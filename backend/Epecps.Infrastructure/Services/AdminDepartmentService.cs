using Epecps.Application.DTOs.Admin;
using Epecps.Application.Interfaces;
using Epecps.Domain.Entities;
using Epecps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Epecps.Infrastructure.Services;

public class AdminDepartmentService : IAdminDepartmentService
{
    private readonly EpecpsDbContext _context;

    public AdminDepartmentService(EpecpsDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetAllDepartmentsAsync()
    {
        var departments = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHodMappings)
                .ThenInclude(m => m.HodUser)
            .ToListAsync();

        return departments.Select(d => new DepartmentDto
        {
            DeptId = d.DeptId,
            Name = d.Name,
            ParentDeptId = d.ParentDeptId,
            ParentDeptName = d.ParentDepartment?.Name,
            Hods = d.DepartmentHodMappings.Select(m => new DepartmentHodDto
            {
                UserId = m.HodUserId,
                FullName = m.HodUser.FullName,
                Email = m.HodUser.Email
            }).ToList()
        }).ToList();
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        var department = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHodMappings)
                .ThenInclude(m => m.HodUser)
            .FirstOrDefaultAsync(d => d.DeptId == id);

        if (department == null) return null;

        return new DepartmentDto
        {
            DeptId = department.DeptId,
            Name = department.Name,
            ParentDeptId = department.ParentDeptId,
            ParentDeptName = department.ParentDepartment?.Name,
            Hods = department.DepartmentHodMappings.Select(m => new DepartmentHodDto
            {
                UserId = m.HodUserId,
                FullName = m.HodUser.FullName,
                Email = m.HodUser.Email
            }).ToList()
        };
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        var department = new Department
        {
            Name = dto.Name,
            ParentDeptId = dto.ParentDeptId
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.DeptId) ?? throw new Exception("Created department not found");
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) throw new KeyNotFoundException($"Department with ID {id} not found");

        department.Name = dto.Name;
        department.ParentDeptId = dto.ParentDeptId;

        await _context.SaveChangesAsync();

        return await GetDepartmentByIdAsync(id) ?? throw new Exception("Updated department not found");
    }

    public async Task DeleteDepartmentAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) throw new KeyNotFoundException($"Department with ID {id} not found");

        // Simple validation to prevent orphaned records (can be expanded)
        bool hasUsers = await _context.Users.AnyAsync(u => u.DeptId == id);
        if (hasUsers) throw new InvalidOperationException("Cannot delete department because it has assigned users.");

        bool hasSubDepartments = await _context.Departments.AnyAsync(d => d.ParentDeptId == id);
        if (hasSubDepartments) throw new InvalidOperationException("Cannot delete department because it has sub-departments.");

        // Clean up mappings first
        var mappings = await _context.DepartmentHodMappings.Where(m => m.DeptId == id).ToListAsync();
        _context.DepartmentHodMappings.RemoveRange(mappings);

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
    }

    public async Task AssignHodAsync(int departmentId, int userId)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null) throw new KeyNotFoundException($"Department with ID {departmentId} not found");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new KeyNotFoundException($"User with ID {userId} not found");

        var existingMapping = await _context.DepartmentHodMappings
            .FirstOrDefaultAsync(m => m.DeptId == departmentId && m.HodUserId == userId);

        if (existingMapping == null)
        {
            _context.DepartmentHodMappings.Add(new DepartmentHodMapping
            {
                DeptId = departmentId,
                HodUserId = userId
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveHodAsync(int departmentId, int userId)
    {
        var mapping = await _context.DepartmentHodMappings
            .FirstOrDefaultAsync(m => m.DeptId == departmentId && m.HodUserId == userId);

        if (mapping != null)
        {
            _context.DepartmentHodMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }
}
