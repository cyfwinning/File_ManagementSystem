using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class CategoryService
{
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Document> _docRepo;

    public CategoryService(IRepository<Category> categoryRepo, IRepository<Document> docRepo)
    {
        _categoryRepo = categoryRepo;
        _docRepo = docRepo;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesBySpaceAsync(long spaceId)
    {
        var categories = await _categoryRepo.Query()
            .Where(c => c.SpaceId == spaceId && c.ParentId == null)
            .Include(c => c.Children).ThenInclude(c => c.Children)
            .Include(c => c.Documents)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return ApiResponse<List<CategoryDto>>.Ok(categories.Select(MapCategory).ToList());
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        int level = 1;
        if (request.ParentId.HasValue)
        {
            var parent = await _categoryRepo.GetByIdAsync(request.ParentId.Value);
            if (parent == null) return ApiResponse<CategoryDto>.Fail("父目录不存在");
            if (parent.Level >= 3) return ApiResponse<CategoryDto>.Fail("最多支持3级目录");
            level = parent.Level + 1;
        }

        // Check duplicate name under the same parent in the same space
        var duplicate = await _categoryRepo.Query()
            .AnyAsync(c => c.SpaceId == request.SpaceId && c.ParentId == request.ParentId && c.Name == request.Name);
        if (duplicate)
            return ApiResponse<CategoryDto>.Fail("同级目录下已存在相同名称的目录");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            SpaceId = request.SpaceId,
            ParentId = request.ParentId,
            Level = level
        };
        await _categoryRepo.AddAsync(category);
        return ApiResponse<CategoryDto>.Ok(new CategoryDto(category.Id, category.Name, category.Description, category.SpaceId, category.ParentId, category.Level, category.SortOrder, null, 0));
    }

    public async Task<ApiResponse> UpdateCategoryAsync(long id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepo.Query()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return ApiResponse.Fail("目录不存在");

        // If renaming, check restrictions
        if (category.Name != request.Name)
        {
            // Check if category has sub-categories
            var hasChildren = category.Children != null && category.Children.Any();
            if (hasChildren)
                return ApiResponse.Fail("该目录下存在子目录，不允许修改名称");

            // Check if category has documents
            var hasDocuments = await _docRepo.Query().AnyAsync(d => d.CategoryId == id);
            if (hasDocuments)
                return ApiResponse.Fail("该目录下存在文件，不允许修改名称");

            // Check duplicate name under the same parent in the same space
            var duplicate = await _categoryRepo.Query()
                .AnyAsync(c => c.SpaceId == category.SpaceId && c.ParentId == category.ParentId && c.Name == request.Name && c.Id != id);
            if (duplicate)
                return ApiResponse.Fail("同级目录下已存在相同名称的目录");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.SortOrder = request.SortOrder;
        await _categoryRepo.UpdateAsync(category);
        return ApiResponse.Ok("更新成功");
    }

    public async Task<ApiResponse> DeleteCategoryAsync(long id)
    {
        var category = await _categoryRepo.Query()
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return ApiResponse.Fail("目录不存在");

        // Check if category has sub-categories
        if (category.Children != null && category.Children.Any())
            return ApiResponse.Fail("该目录下存在子目录，无法删除");

        // Check if category has documents
        var hasDocuments = await _docRepo.Query().AnyAsync(d => d.CategoryId == id);
        if (hasDocuments)
            return ApiResponse.Fail("该目录下存在文件，无法删除");

        await _categoryRepo.DeleteAsync(category);
        return ApiResponse.Ok("删除成功");
    }

    private static CategoryDto MapCategory(Category c) => new(
        c.Id, c.Name, c.Description, c.SpaceId, c.ParentId, c.Level, c.SortOrder,
        c.Children?.OrderBy(ch => ch.SortOrder).Select(MapCategory).ToList(),
        c.Documents?.Count ?? 0);
}
