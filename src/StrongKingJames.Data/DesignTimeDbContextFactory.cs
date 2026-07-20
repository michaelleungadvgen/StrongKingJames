using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace StrongKingJames.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BibleDbContext>
{
    public BibleDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("SKJ_CONNECTION")
                 ?? "Host=localhost;Port=5432;Database=strongkingjames;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<BibleDbContext>()
            .UseNpgsql(cs, o => o.UseVector())
            .Options;
        return new BibleDbContext(options);
    }
}
