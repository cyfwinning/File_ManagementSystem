using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class RecommendationService
{
    private readonly IRepository<Recommendation> _recRepo;
    private readonly IRepository<LearningRecord> _learnRepo;
    private readonly IRepository<User> _userRepo;

    public RecommendationService(IRepository<Recommendation> recRepo, IRepository<LearningRecord> learnRepo, IRepository<User> userRepo)
    {
        _recRepo = recRepo;
        _learnRepo = learnRepo;
        _userRepo = userRepo;
    }

    public async Task<ApiResponse<List<RecommendationDto>>> GetRecommendationsForUserAsync(long userId)
    {
        var user = await _userRepo.Query().Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return ApiResponse<List<RecommendationDto>>.Fail("用户不存在");

        var recs = await _recRepo.Query()
            .Include(r => r.Document).Include(r => r.Recommender)
            .Where(r => r.IsActive && (r.IsCompanyWide || r.TargetUserId == userId || r.TargetDepartmentId == user.DepartmentId))
            .OrderByDescending(r => r.Urgency).ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        return ApiResponse<List<RecommendationDto>>.Ok(recs.Select(r => new RecommendationDto(
            r.Id, r.DocumentId, r.Document?.Title, r.RecommenderId, r.Recommender?.DisplayName,
            r.TargetUserId, r.TargetDepartmentId, r.IsCompanyWide, r.Description, r.IsMandatory,
            r.Urgency.ToString(), r.Deadline, r.IsActive, r.CreatedAt)).ToList());
    }

    public async Task<ApiResponse<RecommendationDto>> CreateRecommendationAsync(CreateRecommendationRequest request, long recommenderId)
    {
        var rec = new Recommendation
        {
            DocumentId = request.DocumentId,
            RecommenderId = recommenderId,
            TargetUserId = request.TargetUserId,
            TargetDepartmentId = request.TargetDepartmentId,
            IsCompanyWide = request.IsCompanyWide,
            Description = request.Description,
            IsMandatory = request.IsMandatory,
            Urgency = request.Urgency,
            Deadline = request.Deadline
        };
        await _recRepo.AddAsync(rec);

        // Create learning records for target users
        if (request.TargetUserId.HasValue)
        {
            await EnsureLearningRecord(request.TargetUserId.Value, request.DocumentId, rec.Id);
        }
        else if (request.TargetDepartmentId.HasValue)
        {
            var users = await _userRepo.FindAsync(u => u.DepartmentId == request.TargetDepartmentId);
            foreach (var u in users)
                await EnsureLearningRecord(u.Id, request.DocumentId, rec.Id);
        }
        else if (request.IsCompanyWide)
        {
            var users = await _userRepo.GetAllAsync();
            foreach (var u in users)
                await EnsureLearningRecord(u.Id, request.DocumentId, rec.Id);
        }

        return ApiResponse<RecommendationDto>.Ok(new RecommendationDto(
            rec.Id, rec.DocumentId, null, rec.RecommenderId, null,
            rec.TargetUserId, rec.TargetDepartmentId, rec.IsCompanyWide, rec.Description,
            rec.IsMandatory, rec.Urgency.ToString(), rec.Deadline, rec.IsActive, rec.CreatedAt));
    }

    private async Task EnsureLearningRecord(long userId, long documentId, long recommendationId)
    {
        var exists = await _learnRepo.AnyAsync(l => l.UserId == userId && l.DocumentId == documentId);
        if (!exists)
        {
            await _learnRepo.AddAsync(new LearningRecord
            {
                UserId = userId,
                DocumentId = documentId,
                RecommendationId = recommendationId,
                Status = LearningStatus.NotStarted
            });
        }
    }
}
