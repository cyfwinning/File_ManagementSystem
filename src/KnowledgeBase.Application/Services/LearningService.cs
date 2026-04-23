using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class LearningService
{
    private readonly IRepository<LearningRecord> _learnRepo;

    public LearningService(IRepository<LearningRecord> learnRepo) => _learnRepo = learnRepo;

    public async Task<ApiResponse<LearningStatsDto>> GetUserStatsAsync(long userId)
    {
        var records = await _learnRepo.FindAsync(l => l.UserId == userId);
        var stats = new LearningStatsDto(
            records.Count,
            records.Count(r => r.Status == LearningStatus.Completed),
            records.Count(r => r.Status == LearningStatus.InProgress),
            records.Count(r => r.Status == LearningStatus.NotStarted),
            records.Sum(r => r.TotalSeconds));
        return ApiResponse<LearningStatsDto>.Ok(stats);
    }

    public async Task<ApiResponse<PagedResult<LearningRecordDto>>> GetUserRecordsAsync(long userId, int page = 1, int pageSize = 20)
    {
        var query = _learnRepo.Query().Include(l => l.Document).Where(l => l.UserId == userId);
        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(l => l.LastAccessAt ?? l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var items = records.Select(l => new LearningRecordDto(l.Id, l.UserId, null, l.DocumentId, l.Document?.Title, l.Status.ToString(), l.ProgressPercent, l.TotalSeconds, l.StartedAt, l.CompletedAt, l.LastAccessAt, l.RecommendationId)).ToList();
        return ApiResponse<PagedResult<LearningRecordDto>>.Ok(new PagedResult<LearningRecordDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    public async Task<ApiResponse> UpdateProgressAsync(UpdateProgressRequest request, long userId)
    {
        var record = (await _learnRepo.FindAsync(l => l.UserId == userId && l.DocumentId == request.DocumentId)).FirstOrDefault();
        if (record == null)
        {
            record = new LearningRecord
            {
                UserId = userId,
                DocumentId = request.DocumentId,
                Status = LearningStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };
            await _learnRepo.AddAsync(record);
        }

        record.ProgressPercent = Math.Min(100, Math.Max(record.ProgressPercent, request.ProgressPercent));
        record.TotalSeconds += request.AdditionalSeconds;
        record.LastAccessAt = DateTime.UtcNow;

        if (record.Status == LearningStatus.NotStarted)
        {
            record.Status = LearningStatus.InProgress;
            record.StartedAt = DateTime.UtcNow;
        }

        if (record.ProgressPercent >= 100)
        {
            record.Status = LearningStatus.Completed;
            record.CompletedAt ??= DateTime.UtcNow;
        }

        await _learnRepo.UpdateAsync(record);
        return ApiResponse.Ok("进度更新成功");
    }

    // Leader view: subordinates learning stats
    public async Task<ApiResponse<List<LearningRecordDto>>> GetSubordinateRecordsAsync(List<long> userIds, long? documentId = null)
    {
        var query = _learnRepo.Query().Include(l => l.User).Include(l => l.Document)
            .Where(l => userIds.Contains(l.UserId));
        if (documentId.HasValue)
            query = query.Where(l => l.DocumentId == documentId);

        var records = await query.OrderByDescending(l => l.LastAccessAt).ToListAsync();
        return ApiResponse<List<LearningRecordDto>>.Ok(records.Select(l =>
            new LearningRecordDto(l.Id, l.UserId, l.User?.DisplayName, l.DocumentId, l.Document?.Title, l.Status.ToString(), l.ProgressPercent, l.TotalSeconds, l.StartedAt, l.CompletedAt, l.LastAccessAt, l.RecommendationId)).ToList());
    }
}
