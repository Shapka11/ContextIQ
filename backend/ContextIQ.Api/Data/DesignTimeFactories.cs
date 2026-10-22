using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContextIQ.Api.Data;

public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteAppDbContext>()
        .UseSqlite(Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? "Data Source=contextiq.db").Options);
}

public sealed class PostgresDesignFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PostgresAppDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Database") ??
            "Host=localhost;Database=contextiq;Username=contextiq;Password=local-only").Options);
}
