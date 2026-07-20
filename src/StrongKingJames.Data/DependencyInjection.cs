using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace StrongKingJames.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddBibleData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BibleDbContext>(opt =>
            opt.UseNpgsql(connectionString, o => o.UseVector()));
        // Repository/search registrations are added in a later task once those classes exist.
        return services;
    }
}
