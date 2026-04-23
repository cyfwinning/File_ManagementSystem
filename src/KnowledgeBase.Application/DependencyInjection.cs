using KnowledgeBase.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<DepartmentService>();
        services.AddScoped<SpaceService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<RecommendationService>();
        services.AddScoped<LearningService>();
        services.AddScoped<ExamService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
