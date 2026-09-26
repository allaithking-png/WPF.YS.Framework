using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AppFramework.Data.Sqlite;

namespace AppFramework.Data.Startup;

/// <summary>
/// يطبّق Migrations على قاعدة البيانات المحلية عند بدء التطبيق.
/// </summary>
public sealed class LocalDbMigrator : IHostedService
{
    private readonly IDbContextFactory<LocalDbContext> _factory;
    private readonly ILogger<LocalDbMigrator> _logger;

    public LocalDbMigrator(
        IDbContextFactory<LocalDbContext> factory,
        ILogger<LocalDbMigrator> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Local database migrated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to migrate local database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}