using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<LearningRecord> LearningRecords => Set<LearningRecord>();
    public DbSet<SpacePermission> SpacePermissions => Set<SpacePermission>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamRecord> ExamRecords => Set<ExamRecord>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<AttachmentPermission> AttachmentPermissions => Set<AttachmentPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(100);
            e.Property(u => u.DisplayName).HasMaxLength(200);
            e.Property(u => u.PasswordHash).HasMaxLength(500);
            e.HasQueryFilter(u => !u.IsDeleted);
            e.HasOne(u => u.Department).WithMany(d => d.Users).HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // Department
        modelBuilder.Entity<Department>(e =>
        {
            e.Property(d => d.Name).HasMaxLength(200);
            e.HasQueryFilter(d => !d.IsDeleted);
            e.HasOne(d => d.Parent).WithMany(d => d.Children).HasForeignKey(d => d.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        // Space
        modelBuilder.Entity<Space>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(200);
            e.HasQueryFilter(s => !s.IsDeleted);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200);
            e.HasQueryFilter(c => !c.IsDeleted);
            e.HasOne(c => c.Space).WithMany(s => s.Categories).HasForeignKey(c => c.SpaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        // Document
        modelBuilder.Entity<Document>(e =>
        {
            e.Property(d => d.Title).HasMaxLength(500);
            e.HasQueryFilter(d => !d.IsDeleted);
            e.HasOne(d => d.Category).WithMany(c => c.Documents).HasForeignKey(d => d.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Space).WithMany().HasForeignKey(d => d.SpaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.Author).WithMany(u => u.CreatedDocuments).HasForeignKey(d => d.AuthorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.Title);
            e.HasIndex(d => d.SpaceId);
        });

        // DocumentVersion
        modelBuilder.Entity<DocumentVersion>(e =>
        {
            e.HasOne(v => v.Document).WithMany(d => d.Versions).HasForeignKey(v => v.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.Editor).WithMany().HasForeignKey(v => v.EditorId).OnDelete(DeleteBehavior.Restrict);
        });

        // Attachment
        modelBuilder.Entity<Attachment>(e =>
        {
            e.Property(a => a.FileName).HasMaxLength(500);
            e.Property(a => a.OriginalFileName).HasMaxLength(500);
            e.HasQueryFilter(a => !a.IsDeleted);
            e.HasOne(a => a.Document).WithMany(d => d.Attachments).HasForeignKey(a => a.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Uploader).WithMany().HasForeignKey(a => a.UploaderId).OnDelete(DeleteBehavior.Restrict);
        });

        // Comment
        modelBuilder.Entity<DocumentComment>(e =>
        {
            e.HasQueryFilter(c => !c.IsDeleted);
            e.HasOne(c => c.Document).WithMany(d => d.Comments).HasForeignKey(c => c.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Parent).WithMany(c => c.Replies).HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        // Recommendation
        modelBuilder.Entity<Recommendation>(e =>
        {
            e.HasQueryFilter(r => !r.IsDeleted);
            e.HasOne(r => r.Document).WithMany(d => d.Recommendations).HasForeignKey(r => r.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Recommender).WithMany().HasForeignKey(r => r.RecommenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.TargetUser).WithMany(u => u.ReceivedRecommendations).HasForeignKey(r => r.TargetUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.TargetDepartment).WithMany().HasForeignKey(r => r.TargetDepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // LearningRecord
        modelBuilder.Entity<LearningRecord>(e =>
        {
            e.HasQueryFilter(l => !l.IsDeleted);
            e.HasOne(l => l.User).WithMany(u => u.LearningRecords).HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Document).WithMany(d => d.LearningRecords).HasForeignKey(l => l.DocumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Recommendation).WithMany().HasForeignKey(l => l.RecommendationId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => new { l.UserId, l.DocumentId });
        });

        // SpacePermission
        modelBuilder.Entity<SpacePermission>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.HasOne(p => p.Space).WithMany(s => s.Permissions).HasForeignKey(p => p.SpaceId).OnDelete(DeleteBehavior.Cascade);
        });

        // OperationLog
        modelBuilder.Entity<OperationLog>(e =>
        {
            e.Property(o => o.Action).HasMaxLength(200);
            e.Property(o => o.Module).HasMaxLength(100);
            e.HasIndex(o => o.CreatedAt);
        });

        // SystemConfig
        modelBuilder.Entity<SystemConfig>(e =>
        {
            e.HasIndex(c => c.ConfigKey).IsUnique();
            e.Property(c => c.ConfigKey).HasMaxLength(200);
        });

        // Favorite
        modelBuilder.Entity<Favorite>(e =>
        {
            e.HasQueryFilter(f => !f.IsDeleted);
            e.HasIndex(f => new { f.UserId, f.DocumentId }).IsUnique();
        });

        // Exam
        modelBuilder.Entity<Exam>(e =>
        {
            e.Property(ex => ex.Title).HasMaxLength(500);
            e.HasQueryFilter(ex => !ex.IsDeleted);
            e.HasOne(ex => ex.Creator).WithMany().HasForeignKey(ex => ex.CreatorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ex => ex.Department).WithMany().HasForeignKey(ex => ex.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // ExamQuestion
        modelBuilder.Entity<ExamQuestion>(e =>
        {
            e.HasOne(q => q.Exam).WithMany(ex => ex.Questions).HasForeignKey(q => q.ExamId).OnDelete(DeleteBehavior.Cascade);
        });

        // ExamRecord
        modelBuilder.Entity<ExamRecord>(e =>
        {
            e.HasOne(r => r.Exam).WithMany(ex => ex.Records).HasForeignKey(r => r.ExamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.ExamId, r.UserId });
        });

        // SearchHistory
        modelBuilder.Entity<SearchHistory>(e =>
        {
            e.Property(s => s.Keyword).HasMaxLength(500);
        });

        // AttachmentPermission
        modelBuilder.Entity<AttachmentPermission>(e =>
        {
            e.HasQueryFilter(p => !p.IsDeleted);
            e.HasOne(p => p.Attachment).WithMany().HasForeignKey(p => p.AttachmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Department).WithMany().HasForeignKey(p => p.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.GrantedBy).WithMany().HasForeignKey(p => p.GrantedById).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}
