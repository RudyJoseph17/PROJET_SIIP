using System.Text;
using BanqueProjet.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BanqueProjet.Infrastructure;

/// <summary>Contexte EF Core en lecture seule sur la vue V_BP_PROJET_VALIDE.</summary>
public class BanqueProjetDbContext : DbContext
{
    public BanqueProjetDbContext(DbContextOptions<BanqueProjetDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<ProjetValide> ProjetsValides => Set<ProjetValide>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProjetValide>().ToView("V_BP_PROJET_VALIDE").HasKey(x => x.IdFiche);
        foreach (var p in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
            p.SetColumnName(VersColonneOracle(p.Name));
    }

    private static string VersColonneOracle(string nom)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < nom.Length; i++)
        {
            if (i > 0 && char.IsUpper(nom[i]) && char.IsLower(nom[i - 1])) sb.Append('_');
            sb.Append(char.ToUpperInvariant(nom[i]));
        }
        return sb.ToString();
    }
}

public sealed class ProjetValideRepository : IProjetValideRepository
{
    private readonly BanqueProjetDbContext _db;
    public ProjetValideRepository(BanqueProjetDbContext db) => _db = db;

    public Task<List<ProjetValide>> ListerAsync(CancellationToken ct = default) =>
        _db.ProjetsValides.OrderByDescending(p => p.DateValidationProjet).ToListAsync(ct);

    public Task<ProjetValide?> ObtenirAsync(int idFiche, CancellationToken ct = default) =>
        _db.ProjetsValides.FirstOrDefaultAsync(p => p.IdFiche == idFiche, ct);
}

public static class DependencyInjection
{
    /// <summary>Enregistre le module Banque de projets (à appeler depuis le Starter).</summary>
    public static IServiceCollection AddBanqueProjet(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BanqueProjetDbContext>(o => o.UseOracle(connectionString));
        services.AddScoped<IProjetValideRepository, ProjetValideRepository>();
        return services;
    }
}
