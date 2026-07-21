using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using StrongKingJames.Core.Services;
using StrongKingJames.Data.Repositories;

namespace StrongKingJames.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddBibleData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BibleDbContext>(opt =>
            opt.UseNpgsql(connectionString, o => o.UseVector()));
        services.AddScoped<IBibleRepository, BibleRepository>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<INoteRepository, NoteRepository>();
        return services;
    }
}
