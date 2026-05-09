using Epecps.Application.DTOs.Admin;

namespace Epecps.Application.Interfaces;

public interface IAdminUserService
{
    Task<List<UserAdminDto>> GetAllUsersAsync();
    Task<UserAdminDto?> GetUserByIdAsync(int id);
    Task<UserAdminDto> CreateUserAsync(CreateUserAdminDto dto);
    Task<UserAdminDto> UpdateUserAsync(int id, UpdateUserAdminDto dto);
    Task DeleteUserAsync(int id);
}
