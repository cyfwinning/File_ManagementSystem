using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace KnowledgeBase.Application.Services;

public class ExamService
{
    private readonly IRepository<Exam> _examRepo;
    private readonly IRepository<ExamQuestion> _questionRepo;
    private readonly IRepository<ExamRecord> _recordRepo;

    public ExamService(IRepository<Exam> examRepo, IRepository<ExamQuestion> questionRepo, IRepository<ExamRecord> recordRepo)
    {
        _examRepo = examRepo;
        _questionRepo = questionRepo;
        _recordRepo = recordRepo;
    }

    public async Task<ApiResponse<PagedResult<ExamDto>>> GetExamsAsync(long? departmentId = null, int page = 1, int pageSize = 20)
    {
        var query = _examRepo.Query().Include(e => e.Department).Include(e => e.Questions).AsQueryable();
        if (departmentId.HasValue) query = query.Where(e => e.DepartmentId == departmentId);

        var total = await query.CountAsync();
        var exams = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var items = exams.Select(e => new ExamDto(e.Id, e.Title, e.Description, e.DepartmentId, e.Department?.Name, e.TimeLimitMinutes, e.StartTime, e.EndTime, e.Status.ToString(), e.TotalScore, e.PassScore, e.Questions.Count, e.CreatedAt)).ToList();
        return ApiResponse<PagedResult<ExamDto>>.Ok(new PagedResult<ExamDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize });
    }

    public async Task<ApiResponse<ExamDetailDto>> GetExamDetailAsync(long examId)
    {
        var exam = await _examRepo.Query().Include(e => e.Questions).FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return ApiResponse<ExamDetailDto>.Fail("考试不存在");

        var questions = exam.Questions.OrderBy(q => q.SortOrder)
            .Select(q => new ExamQuestionDto(q.Id, q.QuestionType.ToString(), q.QuestionText, q.Options, q.Score, q.SortOrder)).ToList();

        return ApiResponse<ExamDetailDto>.Ok(new ExamDetailDto(exam.Id, exam.Title, exam.Description, exam.TimeLimitMinutes, exam.StartTime, exam.EndTime, exam.Status.ToString(), exam.TotalScore, exam.PassScore, questions));
    }

    public async Task<ApiResponse<ExamDto>> CreateExamAsync(CreateExamRequest request, long creatorId)
    {
        var exam = new Exam
        {
            Title = request.Title,
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            CreatorId = creatorId,
            TimeLimitMinutes = request.TimeLimitMinutes,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            PassScore = request.PassScore,
            TotalScore = request.Questions.Sum(q => q.Score),
            Status = ExamStatus.Draft
        };
        await _examRepo.AddAsync(exam);

        var questions = request.Questions.Select(q => new ExamQuestion
        {
            ExamId = exam.Id,
            QuestionType = q.QuestionType,
            QuestionText = q.QuestionText,
            Options = q.Options,
            CorrectAnswer = q.CorrectAnswer,
            Score = q.Score,
            SortOrder = q.SortOrder
        });
        await _questionRepo.AddRangeAsync(questions);

        return ApiResponse<ExamDto>.Ok(new ExamDto(exam.Id, exam.Title, exam.Description, exam.DepartmentId, null, exam.TimeLimitMinutes, exam.StartTime, exam.EndTime, exam.Status.ToString(), exam.TotalScore, exam.PassScore, request.Questions.Count, exam.CreatedAt));
    }

    public async Task<ApiResponse<ExamResultDto>> SubmitExamAsync(SubmitExamRequest request, long userId, int timeSpentSeconds)
    {
        var exam = await _examRepo.Query().Include(e => e.Questions).FirstOrDefaultAsync(e => e.Id == request.ExamId);
        if (exam == null) return ApiResponse<ExamResultDto>.Fail("考试不存在");

        int score = 0;
        foreach (var question in exam.Questions)
        {
            if (request.Answers.TryGetValue(question.Id, out var answer) && answer == question.CorrectAnswer)
                score += question.Score;
        }

        var record = new ExamRecord
        {
            ExamId = exam.Id,
            UserId = userId,
            Answers = JsonSerializer.Serialize(request.Answers),
            TotalScore = score,
            IsPassed = score >= exam.PassScore,
            SubmittedAt = DateTime.UtcNow,
            TimeSpentSeconds = timeSpentSeconds
        };
        await _recordRepo.AddAsync(record);

        return ApiResponse<ExamResultDto>.Ok(new ExamResultDto(exam.Id, exam.TotalScore, score, record.IsPassed, timeSpentSeconds));
    }
}
