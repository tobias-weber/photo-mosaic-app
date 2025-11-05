using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace backend.Helpers;

public static class DatabaseHelper
{
    public static void EnsureDatabaseAndDirectoryCreated<TContext>(TContext context)
        where TContext : DbContext
    {

        if (IsSqlite(context.Database))
        {
            // Extract the path from connection string
            var connectionString = context.Database.GetDbConnection().ConnectionString;
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var dbPath = builder.DataSource;

            // Ensure directory exists
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        // Create DB if missing
        context.Database.EnsureCreated(); // could be changed to use migrations for production
    }

    public static bool IsSqlite(DatabaseFacade database)
    {
        var provider = database.ProviderName;
        return provider != null && provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
    }
}