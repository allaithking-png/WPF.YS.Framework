using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Data.Sqlite;
using AppFramework.Data.Storage;
using AppFramework.Abstractions.Services;
using AppFramework.Data.Services;


namespace AppFramework.Data.DependencyInjection;

/// <summary>
/// امتدادات DI لطبقة البيانات.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل طبقة البيانات المحلية (SQLite + Outbox).
    /// </summary>
    /// <param name="services">حاوية الخدمات.</param>
    /// <param name="connectionString">
    /// سلسلة الاتصال. إن كانت null، يُستخدم مسار افتراضي في %APPDATA%.
    /// </param>
    public static IServiceCollection AddAppFrameworkData(
        this IServiceCollection services,
        string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // استخدم مسارًا افتراضيًا إن لم يُحدَّد
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = GetDefaultConnectionString();
        }

        // تأكد من وجود المجلد
        EnsureDatabaseFolderExists(connectionString);

        // DbContextFactory (آمن للـ Threads المتعددة)
        services.AddDbContextFactory<LocalDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(false);
        });

        // المخزن المحلي
        services.AddSingleton<ILocalStore, SqliteLocalStore>();
        services.AddSingleton<IDataService, DataService>();

        // ✅ طبّق Migrations عند بدء الـ Host
        services.AddHostedService<Startup.LocalDbMigrator>();

        return services;


        
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private static string GetDefaultConnectionString()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "WPF.YS.Framework");
        var dbPath = Path.Combine(folder, "data.db");
        return $"Data Source={dbPath}";
    }

    private static void EnsureDatabaseFolderExists(string connectionString)
    {
        try
        {
            // استخرج المسار من "Data Source=..." 
            var parts = connectionString.Split('=', 2);
            if (parts.Length != 2) return;

            var dbPath = parts[1].Trim();
            var folder = Path.GetDirectoryName(dbPath);

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        catch
        {
            // تجاهل — EF سيتعامل مع الأخطاء
        }
    }
}
