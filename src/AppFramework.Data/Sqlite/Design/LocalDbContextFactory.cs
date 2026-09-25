using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AppFramework.Data.Sqlite.Design;

/// <summary>
/// مصنع DbContext للاستخدام عند التصميم (Migrations).
/// لا يُستخدم في الإنتاج.
/// </summary>
public sealed class LocalDbContextFactory : IDesignTimeDbContextFactory<LocalDbContext>
{
    public LocalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new LocalDbContext(options);
    }
}