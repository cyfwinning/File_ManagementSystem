using KnowledgeBase.Domain.Interfaces;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Repositories;
using KnowledgeBase.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database config manager (singleton, reads from dbconfig.json)
        var basePath = Directory.GetCurrentDirectory();
        var dbConfigManager = new DatabaseConfigManager(basePath);
        services.AddSingleton(dbConfigManager);

        // Scoped database session - holds the DB type for the current request
        services.AddScoped<DatabaseSession>();

        // Register AppDbContext as scoped with dynamic provider resolution
        services.AddScoped<AppDbContext>(sp =>
        {
            var session = sp.GetRequiredService<DatabaseSession>();
            var configManager = sp.GetRequiredService<DatabaseConfigManager>();
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var dbType = session.DatabaseType;
            var connStr = configManager.GetConnectionString(dbType);

            if (dbType.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                // Use fixed version to avoid connecting to MySQL during DI resolution.
                // ServerVersion.AutoDetect requires a live connection which fails if MySQL is down.
                optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 0)));
            }
            else
            {
                optionsBuilder.UseSqlite(connStr);
            }

            return new AppDbContext(optionsBuilder.Options);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        var storagePath = configuration.GetValue<string>("FileStorage:BasePath") ?? Path.Combine(basePath, "uploads");
        var baseUrl = configuration.GetValue<string>("FileStorage:BaseUrl") ?? "http://localhost:5000";
        services.AddSingleton<IFileStorageService>(new LocalFileStorageService(storagePath, baseUrl));

        return services;
    }
}
