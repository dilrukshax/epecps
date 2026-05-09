using Epecps.Application.DTOs.Admin;

namespace Epecps.Application.Interfaces;

public interface IAdminDepartmentService
{
    Task<List<DepartmentDto>> GetAllDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto);
    Task<DepartmentDto> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto);
    Task DeleteDepartmentAsync(int id);
    
    Task AssignHodAsync(int departmentId, int userId);
    Task RemoveHodAsync(int departmentId, int userId);
}
