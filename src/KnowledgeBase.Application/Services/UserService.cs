using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class UserService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Department> _deptRepo;

    public UserService(IRepository<User> userRepo, IRepository<Department> deptRepo)
    {
        _userRepo = userRepo;
        _deptRepo = deptRepo;
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var query = _userRepo.Query();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.Username.Contains(keyword) || u.DisplayName.Contains(keyword));

        var total = await query.CountAsync();
        var users = await query
            .Include(u => u.Department)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.Email, u.Phone, u.Avatar, u.Role.ToString(), u.DepartmentId, u.Department != null ? u.Department.Name : null, u.IsActive))
            .ToListAsync();

        return ApiResponse<PagedResult<UserDto>>.Ok(new PagedResult<UserDto> { Items = users, TotalCount = total, Page = page, PageSize = pageSize });
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(long id)
    {
        var user = await _userRepo.Query().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return ApiResponse<UserDto>.Fail("用户不存在");
        return ApiResponse<UserDto>.Ok(new UserDto(user.Id, user.Username, user.DisplayName, user.Email, user.Phone, user.Avatar, user.Role.ToString(), user.DepartmentId, user.Department?.Name, user.IsActive));
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        if (await _userRepo.AnyAsync(u => u.Username == request.Username))
            return ApiResponse<UserDto>.Fail("用户名已存在");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            DepartmentId = request.DepartmentId
        };
        await _userRepo.AddAsync(user);
        return ApiResponse<UserDto>.Ok(new UserDto(user.Id, user.Username, user.DisplayName, user.Email, user.Phone, user.Avatar, user.Role.ToString(), user.DepartmentId, null, user.IsActive));
    }

    public async Task<ApiResponse> UpdateUserAsync(long id, UpdateUserRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ApiResponse.Fail("用户不存在");

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Role = request.Role;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        await _userRepo.UpdateAsync(user);
        return ApiResponse.Ok("更新成功");
    }

    public async Task<ApiResponse> DeleteUserAsync(long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return ApiResponse.Fail("用户不存在");
        await _userRepo.DeleteAsync(user);
        return ApiResponse.Ok("删除成功");
    }
}
