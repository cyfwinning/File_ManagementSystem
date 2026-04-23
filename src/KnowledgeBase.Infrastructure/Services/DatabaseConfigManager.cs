using System.Text.Json;

namespace KnowledgeBase.Infrastructure.Services;

public class DatabaseConfigFile
{
    public bool EnableSQLite { get; set; } = true;
    public bool EnableMySQL { get; set; } = false;
    public string DefaultType { get; set; } = "SQLite";
    public string SQLiteConnectionString { get; set; } = "Data Source=knowledgebase.db";
    public string MySQLConnectionString { get; set; } = "Server=localhost;Port=3306;Database=knowledgebase;User=root;Password=root;";
}

public class DatabaseConfigManager
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public DatabaseConfigManager(string basePath)
    {
        _configPath = Path.Combine(basePath, "dbconfig.json");
    }

    public DatabaseConfigFile Load()
    {
        if (!File.Exists(_configPath))
        {
            var defaultConfig = new DatabaseConfigFile();
            Save(defaultConfig);
            return defaultConfig;
        }

        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<DatabaseConfigFile>(json) ?? new DatabaseConfigFile();
    }

    public void Save(DatabaseConfigFile config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    /// <summary>
    /// Returns the list of enabled database types.
    /// </summary>
    public List<string> GetEnabledTypes()
    {
        var config = Load();
        var types = new List<string>();
        if (config.EnableSQLite) types.Add("SQLite");
        if (config.EnableMySQL) types.Add("MySQL");
        if (types.Count == 0) types.Add("SQLite"); // fallback
        return types;
    }

    /// <summary>
    /// Returns the default database type.
    /// </summary>
    public string GetDefaultType()
    {
        var config = Load();
        var enabled = GetEnabledTypes();
        if (enabled.Contains(config.DefaultType)) return config.DefaultType;
        return enabled[0];
    }

    /// <summary>
    /// Returns the connection string for the given database type.
    /// </summary>
    public string GetConnectionString(string dbType)
    {
        var config = Load();
        return dbType.Equals("MySQL", StringComparison.OrdinalIgnoreCase)
            ? config.MySQLConnectionString
            : config.SQLiteConnectionString;
    }
}
