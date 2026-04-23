using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class SpaceService
{
    private readonly IRepository<Space> _spaceRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Document> _docRepo;

    public SpaceService(IRepository<Space> spaceRepo, IRepository<Category> categoryRepo, IRepository<Document> docRepo)
    {
        _spaceRepo = spaceRepo;
        _categoryRepo = categoryRepo;
        _docRepo = docRepo;
    }

    public async Task<ApiResponse<List<SpaceDto>>> GetSpacesAsync(long? userId = null)
    {
        var query = _spaceRepo.Query().Include(s => s.Owner).AsQueryable();
        var spaces = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();

        var result = new List<SpaceDto>();
        foreach (var s in spaces)
        {
            var docCount = await _docRepo.CountAsync(d => d.SpaceId == s.Id);
            result.Add(new SpaceDto(s.Id, s.Name, s.Description, s.CoverImage, s.Type.ToString(), s.Visibility.ToString(), s.OwnerId, s.Owner?.DisplayName, s.DepartmentId, docCount));
        }
        return ApiResponse<List<SpaceDto>>.Ok(result);
    }

    public async Task<ApiResponse<SpaceDto>> CreateSpaceAsync(CreateSpaceRequest request, long userId)
    {
        var space = new Space
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Visibility = request.Visibility,
            OwnerId = userId,
            DepartmentId = request.DepartmentId,
            Template = request.Template
        };
        await _spaceRepo.AddAsync(space);

        // Create default root category
        await _categoryRepo.AddAsync(new Category { Name = "默认目录", SpaceId = space.Id, Level = 1, SortOrder = 0 });

        return ApiResponse<SpaceDto>.Ok(new SpaceDto(space.Id, space.Name, space.Description, space.CoverImage, space.Type.ToString(), space.Visibility.ToString(), space.OwnerId, null, space.DepartmentId, 0));
    }

    public async Task<ApiResponse> UpdateSpaceAsync(long id, UpdateSpaceRequest request)
    {
        var space = await _spaceRepo.GetByIdAsync(id);
        if (space == null) return ApiResponse.Fail("空间不存在");

        space.Name = request.Name;
        space.Description = request.Description;
        space.Visibility = request.Visibility;
        await _spaceRepo.UpdateAsync(space);
        return ApiResponse.Ok("更新成功");
    }

    public async Task<ApiResponse> DeleteSpaceAsync(long id)
    {
        var space = await _spaceRepo.GetByIdAsync(id);
        if (space == null) return ApiResponse.Fail("空间不存在");
        await _spaceRepo.DeleteAsync(space);
        return ApiResponse.Ok("删除成功");
    }
}
