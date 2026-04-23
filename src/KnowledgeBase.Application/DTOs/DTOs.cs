using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Application.DTOs;

// === Auth ===
public record LoginRequest(string Username, string Password, string? DatabaseType = null);
public record LoginResponse(string Token, string DisplayName, string Role, long UserId, string DatabaseType);

// === Database Options ===
public record DatabaseOptionsDto(List<string> EnabledTypes, string DefaultType);

// === User ===
public record UserDto(long Id, string Username, string DisplayName, string? Email, string? Phone, string? Avatar, string Role, long? DepartmentId, string? DepartmentName, bool IsActive);
public record CreateUserRequest(string Username, string Password, string DisplayName, string? Email, string? Phone, UserRole Role, long? DepartmentId);
public record UpdateUserRequest(string DisplayName, string? Email, string? Phone, UserRole Role, long? DepartmentId, bool IsActive);

// === Department ===
public record DepartmentDto(long Id, string Name, string? Description, long? ParentId, int SortOrder, List<DepartmentDto>? Children);
public record CreateDepartmentRequest(string Name, string? Description, long? ParentId, int SortOrder);
public record UpdateDepartmentRequest(string Name, string? Description, int SortOrder);

// === Space ===
public record SpaceDto(long Id, string Name, string? Description, string? CoverImage, string Type, string Visibility, long OwnerId, string? OwnerName, long? DepartmentId, int DocumentCount);
public record CreateSpaceRequest(string Name, string? Description, SpaceType Type, SpaceVisibility Visibility, long? DepartmentId, string? Template);
public record UpdateSpaceRequest(string Name, string? Description, SpaceVisibility Visibility);

// === Category ===
public record CategoryDto(long Id, string Name, string? Description, long SpaceId, long? ParentId, int Level, int SortOrder, List<CategoryDto>? Children, int DocumentCount);
public record CreateCategoryRequest(string Name, string? Description, long SpaceId, long? ParentId);
public record UpdateCategoryRequest(string Name, string? Description, int SortOrder);

// === Document ===
public record DocumentDto(long Id, string Title, string? OriginalFileName, string? CoverUrl, string? Content, string? Summary, string Status, string EditMode, long CategoryId, string? CategoryName, long SpaceId, string? SpaceName, long AuthorId, string? AuthorName, string? Tags, bool IsPinned, int ViewCount, int FavoriteCount, int EditCount, int CurrentVersion, DateTime CreatedAt, DateTime? UpdatedAt);
public record DocumentListDto(long Id, string Title, string? OriginalFileName, string? CoverUrl, string? Summary, string Status, string? AuthorName, string? Tags, bool IsPinned, int ViewCount, int FavoriteCount, DateTime CreatedAt, DateTime? UpdatedAt);
public record CreateDocumentRequest(string Title, string? OriginalFileName, string? Content, string? Summary, DocumentEditMode EditMode, long CategoryId, long SpaceId, string? Tags);
public record UpdateDocumentRequest(string Title, string? Content, string? Summary, string? Tags, string? ChangeNote);

// === Attachment ===
public record AttachmentDto(long Id, string OriginalFileName, string ContentType, string FileExtension, long FileSize, string Type, long DocumentId, string? UploaderName, string Url, DateTime CreatedAt, bool CanPreview = true, bool CanDownload = true);

// === Attachment Permission ===
public record AttachmentPermissionDto(long Id, long AttachmentId, string? AttachmentName, long? UserId, string? UserName, long? DepartmentId, string? DepartmentName, bool CanPreview, bool CanDownload);
public record SetAttachmentPermissionRequest(long AttachmentId, long? UserId, long? DepartmentId, bool CanPreview, bool CanDownload);

// === Comment ===
public record CommentDto(long Id, long DocumentId, long UserId, string? UserName, string Content, long? ParentId, string? MentionedUserIds, DateTime CreatedAt, List<CommentDto>? Replies);
public record CreateCommentRequest(long DocumentId, string Content, long? ParentId, string? MentionedUserIds);

// === Recommendation ===
public record RecommendationDto(long Id, long DocumentId, string? DocumentTitle, long RecommenderId, string? RecommenderName, long? TargetUserId, long? TargetDepartmentId, bool IsCompanyWide, string? Description, bool IsMandatory, string Urgency, DateTime? Deadline, bool IsActive, DateTime CreatedAt);
public record CreateRecommendationRequest(long DocumentId, long? TargetUserId, long? TargetDepartmentId, bool IsCompanyWide, string? Description, bool IsMandatory, UrgencyLevel Urgency, DateTime? Deadline);

// === Learning ===
public record LearningRecordDto(long Id, long UserId, string? UserName, long DocumentId, string? DocumentTitle, string Status, int ProgressPercent, int TotalSeconds, DateTime? StartedAt, DateTime? CompletedAt, DateTime? LastAccessAt, long? RecommendationId);
public record UpdateProgressRequest(long DocumentId, int ProgressPercent, int AdditionalSeconds);
public record LearningStatsDto(int TotalDocuments, int CompletedDocuments, int InProgressDocuments, int NotStartedDocuments, int TotalSeconds);

// === Exam ===
public record ExamDto(long Id, string Title, string? Description, long? DepartmentId, string? DepartmentName, int TimeLimitMinutes, DateTime? StartTime, DateTime? EndTime, string Status, int TotalScore, int PassScore, int QuestionCount, DateTime CreatedAt);
public record ExamDetailDto(long Id, string Title, string? Description, int TimeLimitMinutes, DateTime? StartTime, DateTime? EndTime, string Status, int TotalScore, int PassScore, List<ExamQuestionDto> Questions);
public record ExamQuestionDto(long Id, string QuestionType, string QuestionText, string? Options, int Score, int SortOrder);
public record CreateExamRequest(string Title, string? Description, long? DepartmentId, int TimeLimitMinutes, DateTime? StartTime, DateTime? EndTime, int PassScore, List<CreateExamQuestionRequest> Questions);
public record CreateExamQuestionRequest(ExamQuestionType QuestionType, string QuestionText, string? Options, string CorrectAnswer, int Score, int SortOrder);
public record SubmitExamRequest(long ExamId, Dictionary<long, string> Answers);
public record ExamResultDto(long ExamId, int TotalScore, int UserScore, bool IsPassed, int TimeSpentSeconds);

// === Search ===
public record SearchRequest(string? Keyword, string? SpaceId, string? Tag, string? Author, DateTime? StartDate, DateTime? EndDate, string? SortBy, int Page = 1, int PageSize = 20);
public record SearchResultDto(long Id, string Title, string? Summary, string? Highlight, string? AuthorName, string? SpaceName, DateTime CreatedAt, double Score);

// === Dashboard ===
public record DashboardPersonalStatsDto(
    int YearReadCount, int YearReadSeconds,
    int WeekReadCount, int WeekReadSeconds,
    int Last7DaysReadCount, int Last7DaysReadSeconds,
    int TotalReadCount, int TotalReadSeconds);

public record TrendPointDto(string Label, int ReadCount, int ReadSeconds);

public record DashboardTrendDto(string Period, List<TrendPointDto> Points);

public record DepartmentUserStatsDto(long UserId, string DisplayName, int ReadCount, int ReadSeconds, int CompletedCount, DateTime? LastAccessAt);

public record DashboardDepartmentDto(
    string DepartmentName,
    int TotalMembers,
    int TotalReadCount, int TotalReadSeconds, int TotalCompletedCount,
    List<DepartmentUserStatsDto> Members);

// === System Config ===
public record SystemConfigDto(long Id, string ConfigKey, string ConfigValue, string? Description, string? Group);
public record DatabaseConfigRequest(string Type, string? Server, string? Database, string? Username, string? Password, int? Port);
