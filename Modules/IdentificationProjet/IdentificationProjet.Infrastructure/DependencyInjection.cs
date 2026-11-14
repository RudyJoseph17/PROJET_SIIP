using IdentificationProjet.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentificationProjet.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Enregistre le module "Identification projet" (à appeler depuis le Starter).</summary>
    public static IServiceCollection AddIdentificationProjet(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdentificationProjetDbContext>(o => o.UseOracle(connectionString));
        services.AddScoped<IFicheIdentificationRepository, FicheIdentificationRepository>();
        return services;
    }
}
