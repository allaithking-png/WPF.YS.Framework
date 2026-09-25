using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AppFramework.Data.Sqlite;

namespace AppFramework.Data.Tests.TestHelpers;

/// <summary>
/// مصنع DbContext للاختبارات — يستخدم SQLite InMemory.
/// </summary>
public sealed class InMemoryDbFactory : IDbContextFactory<LocalDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LocalDbContext> _options;

    public InMemoryDbFactory()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(_connection)
            .Options;

        // أنشئ الـ Schema
        using var db = new LocalDbContext(_options);
        db.Database.EnsureCreated();
    }

    public LocalDbContext CreateDbContext() => new(_options);

    public void Dispose()
    {
        _connection.Dispose();
    }
}