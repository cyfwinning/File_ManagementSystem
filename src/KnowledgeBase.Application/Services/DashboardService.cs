using KnowledgeBase.Application.Common;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Application.Services;

public class DashboardService
{
    private readonly IRepository<LearningRecord> _learnRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Department> _deptRepo;

    public DashboardService(
        IRepository<LearningRecord> learnRepo,
        IRepository<User> userRepo,
        IRepository<Department> deptRepo)
    {
        _learnRepo = learnRepo;
        _userRepo = userRepo;
        _deptRepo = deptRepo;
    }

    public async Task<ApiResponse<DashboardPersonalStatsDto>> GetPersonalStatsAsync(long userId)
    {
        var now = DateTime.UtcNow;
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        if (now.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
        weekStart = DateTime.SpecifyKind(weekStart, DateTimeKind.Utc);
        var last7Days = DateTime.SpecifyKind(now.Date.AddDays(-6), DateTimeKind.Utc);

        var records = await _learnRepo.Query()
            .Where(l => l.UserId == userId)
            .ToListAsync();

        var yearRecords = records.Where(r => (r.LastAccessAt ?? r.CreatedAt) >= yearStart).ToList();
        var weekRecords = records.Where(r => (r.LastAccessAt ?? r.CreatedAt) >= weekStart).ToList();
        var last7Records = records.Where(r => (r.LastAccessAt ?? r.CreatedAt) >= last7Days).ToList();

        var stats = new DashboardPersonalStatsDto(
            yearRecords.Count, yearRecords.Sum(r => r.TotalSeconds),
            weekRecords.Count, weekRecords.Sum(r => r.TotalSeconds),
            last7Records.Count, last7Records.Sum(r => r.TotalSeconds),
            records.Count, records.Sum(r => r.TotalSeconds));

        return ApiResponse<DashboardPersonalStatsDto>.Ok(stats);
    }

    public async Task<ApiResponse<DashboardTrendDto>> GetTrendAsync(long userId, string period, string? role = null, long? departmentId = null)
    {
        var now = DateTime.UtcNow;
        var points = new List<TrendPointDto>();

        // For leaders, aggregate across department; for personal, use userId
        IQueryable<LearningRecord> baseQuery;
        if (role != null && IsLeaderRole(role) && departmentId.HasValue)
        {
            var memberIds = await _userRepo.Query()
                .Where(u => u.DepartmentId == departmentId.Value && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();
            baseQuery = _learnRepo.Query().Where(l => memberIds.Contains(l.UserId));
        }
        else
        {
            baseQuery = _learnRepo.Query().Where(l => l.UserId == userId);
        }

        switch (period.ToLower())
        {
            case "year":
                // Last 12 months
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1).AddMonths(-i), DateTimeKind.Utc);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthRecords = await baseQuery
                        .Where(l => (l.LastAccessAt ?? l.CreatedAt) >= monthStart && (l.LastAccessAt ?? l.CreatedAt) < monthEnd)
                        .ToListAsync();
                    points.Add(new TrendPointDto(monthStart.ToString("yyyy-MM"), monthRecords.Count, monthRecords.Sum(r => r.TotalSeconds)));
                }
                break;

            case "month":
                // Last 30 days
                for (int i = 29; i >= 0; i--)
                {
                    var dayStart = DateTime.SpecifyKind(now.Date.AddDays(-i), DateTimeKind.Utc);
                    var dayEnd = dayStart.AddDays(1);
                    var dayRecords = await baseQuery
                        .Where(l => (l.LastAccessAt ?? l.CreatedAt) >= dayStart && (l.LastAccessAt ?? l.CreatedAt) < dayEnd)
                        .ToListAsync();
                    points.Add(new TrendPointDto(dayStart.ToString("MM-dd"), dayRecords.Count, dayRecords.Sum(r => r.TotalSeconds)));
                }
                break;

            case "week":
                // Last 7 days by day
                for (int i = 6; i >= 0; i--)
                {
                    var dayStart = DateTime.SpecifyKind(now.Date.AddDays(-i), DateTimeKind.Utc);
                    var dayEnd = dayStart.AddDays(1);
                    var dayRecords = await baseQuery
                        .Where(l => (l.LastAccessAt ?? l.CreatedAt) >= dayStart && (l.LastAccessAt ?? l.CreatedAt) < dayEnd)
                        .ToListAsync();
                    var dayNames = new[] { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
                    points.Add(new TrendPointDto($"{dayStart:MM-dd}({dayNames[(int)dayStart.DayOfWeek]})", dayRecords.Count, dayRecords.Sum(r => r.TotalSeconds)));
                }
                break;

            default: // "7day"
                for (int i = 6; i >= 0; i--)
                {
                    var dayStart = DateTime.SpecifyKind(now.Date.AddDays(-i), DateTimeKind.Utc);
                    var dayEnd = dayStart.AddDays(1);
                    var dayRecords = await baseQuery
                        .Where(l => (l.LastAccessAt ?? l.CreatedAt) >= dayStart && (l.LastAccessAt ?? l.CreatedAt) < dayEnd)
                        .ToListAsync();
                    points.Add(new TrendPointDto(dayStart.ToString("MM-dd"), dayRecords.Count, dayRecords.Sum(r => r.TotalSeconds)));
                }
                break;
        }

        return ApiResponse<DashboardTrendDto>.Ok(new DashboardTrendDto(period, points));
    }

    public async Task<ApiResponse<DashboardDepartmentDto>> GetDepartmentDashboardAsync(long departmentId)
    {
        var dept = await _deptRepo.GetByIdAsync(departmentId);
        var members = await _userRepo.Query()
            .Where(u => u.DepartmentId == departmentId && u.IsActive)
            .ToListAsync();

        var memberIds = members.Select(u => u.Id).ToList();
        var records = await _learnRepo.Query()
            .Where(l => memberIds.Contains(l.UserId))
            .ToListAsync();

        var memberStats = members.Select(m =>
        {
            var userRecords = records.Where(r => r.UserId == m.Id).ToList();
            return new DepartmentUserStatsDto(
                m.Id, m.DisplayName,
                userRecords.Count,
                userRecords.Sum(r => r.TotalSeconds),
                userRecords.Count(r => r.Status == LearningStatus.Completed),
                userRecords.Max(r => r.LastAccessAt));
        }).OrderByDescending(s => s.ReadSeconds).ToList();

        var result = new DashboardDepartmentDto(
            dept?.Name ?? "未知部门",
            members.Count,
            records.Count,
            records.Sum(r => r.TotalSeconds),
            records.Count(r => r.Status == LearningStatus.Completed),
            memberStats);

        return ApiResponse<DashboardDepartmentDto>.Ok(result);
    }

    public async Task<ApiResponse<DashboardPersonalStatsDto>> GetUserStatsDetailAsync(long targetUserId)
    {
        return await GetPersonalStatsAsync(targetUserId);
    }

    private static bool IsLeaderRole(string role)
    {
        return role is "SuperAdmin" or "CompanyLeader" or "DepartmentLeader";
    }
}
