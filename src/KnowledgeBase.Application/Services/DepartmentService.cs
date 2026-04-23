using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class DepartmentService
{
    private readonly IRepository<Department> _deptRepo;
    private readonly IRepository<User> _userRepo;

    public DepartmentService(IRepository<Department> deptRepo, IRepository<User> userRepo)
    {
        _deptRepo = deptRepo;
        _userRepo = userRepo;
    }

    public async Task<ApiResponse<List<DepartmentDto>>> GetDepartmentTreeAsync()
    {
        var depts = await _deptRepo.Query()
            .Where(d => d.ParentId == null)
            .Include(d => d.Children).ThenInclude(d => d.Children).ThenInclude(d => d.Children)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();

        return ApiResponse<List<DepartmentDto>>.Ok(depts.Select(MapDepartment).ToList());
    }

    public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        // Check duplicate name under the same parent
        var duplicate = await _deptRepo.Query()
            .AnyAsync(d => d.ParentId == request.ParentId && d.Name == request.Name);
        if (duplicate)
            return ApiResponse<DepartmentDto>.Fail("同级下已存在相同名称的部门");

        var dept = new Department
        {
            Name = request.Name,
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder
        };
        await _deptRepo.AddAsync(dept);
        return ApiResponse<DepartmentDto>.Ok(new DepartmentDto(dept.Id, dept.Name, dept.Description, dept.ParentId, dept.SortOrder, null));
    }

    public async Task<ApiResponse<DepartmentDto>> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request)
    {
        var dept = await _deptRepo.Query()
            .Include(d => d.Children)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dept == null) return ApiResponse<DepartmentDto>.Fail("部门不存在");

        // If renaming, check for duplicates under the same parent
        if (dept.Name != request.Name)
        {
            var duplicate = await _deptRepo.Query()
                .AnyAsync(d => d.ParentId == dept.ParentId && d.Name == request.Name && d.Id != id);
            if (duplicate)
                return ApiResponse<DepartmentDto>.Fail("同级下已存在相同名称的部门");
        }

        dept.Name = request.Name;
        dept.Description = request.Description;
        dept.SortOrder = request.SortOrder;
        await _deptRepo.UpdateAsync(dept);
        return ApiResponse<DepartmentDto>.Ok(new DepartmentDto(dept.Id, dept.Name, dept.Description, dept.ParentId, dept.SortOrder, null));
    }

    public async Task<ApiResponse> DeleteDepartmentAsync(long id)
    {
        var dept = await _deptRepo.Query()
            .Include(d => d.Children)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dept == null) return ApiResponse.Fail("部门不存在");

        // Check if department has sub-departments
        if (dept.Children != null && dept.Children.Any())
            return ApiResponse.Fail("该部门下存在子部门，无法删除");

        // Check if department has users
        var hasUsers = await _userRepo.Query().AnyAsync(u => u.DepartmentId == id);
        if (hasUsers)
            return ApiResponse.Fail("该部门下存在用户，无法删除");

        await _deptRepo.DeleteAsync(dept);
        return ApiResponse.Ok("删除成功");
    }

    private static DepartmentDto MapDepartment(Department d) => new(
        d.Id, d.Name, d.Description, d.ParentId, d.SortOrder,
        d.Children?.OrderBy(c => c.SortOrder).Select(MapDepartment).ToList());
}
