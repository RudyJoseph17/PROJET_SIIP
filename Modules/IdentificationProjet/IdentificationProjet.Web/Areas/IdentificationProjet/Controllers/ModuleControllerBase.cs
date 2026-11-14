using System.Text.Json;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Shared.Domain.Entities;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Controllers;

/// <summary>Base commune aux contrôleurs du module : utilisateur courant et référentiels.</summary>
[Area("IdentificationProjet")]
[Authorize]
public abstract class ModuleControllerBase : Controller
{
    private const string CleCacheLieux = "IdentificationProjet.Lieux";
    private ContexteUtilisateur? _utilisateur;

    protected UserManager<ApplicationUser> Users { get; }

    protected ModuleControllerBase(UserManager<ApplicationUser> users) => Users = users;

    protected async Task<ContexteUtilisateur> UtilisateurAsync()
    {
        if (_utilisateur is not null) return _utilisateur;
        var user = await Users.GetUserAsync(User);
        var nom = user is null ? null : $"{user.Prenom} {user.Nom}".Trim();
        _utilisateur = new ContexteUtilisateur(
            UserId: user?.Id ?? Users.GetUserId(User) ?? "",
            NomAffiche: string.IsNullOrWhiteSpace(nom) ? User.Identity?.Name ?? "" : nom,
            Ministere: user?.Ministere,
            Courriel: user?.Email,
            EstUep: User.IsInRole(RolesIdentification.Uep),
            EstAnalyste: User.IsInRole(RolesIdentification.DipAnalyste),
            EstChefService: User.IsInRole(RolesIdentification.DipChefService),
            EstDirecteur: User.IsInRole(RolesIdentification.DipDirecteur),
            EstConsultantDip: User.IsInRole(RolesIdentification.Dip),
            EstAdmin: User.IsInRole(RolesIdentification.Admin));
        ViewData["Utilisateur"] = _utilisateur;
        return _utilisateur;
    }

    /// <summary>
    /// Référentiel géographique au format JSON compact pour les listes en cascade :
    /// { "<idParent ou 0>": [[id, "libellé"], ...] } — mis en cache 6 heures.
    /// </summary>
    protected async Task<string> LieuxJsonAsync(IFicheIdentificationRepository repo, IMemoryCache cache, CancellationToken ct)
    {
        if (cache.TryGetValue(CleCacheLieux, out string? json) && json is not null) return json;
        var lieux = await repo.ListerLieuxAsync(ct);
        var arbre = lieux.GroupBy(l => (l.IdParent ?? 0).ToString())
                         .ToDictionary(g => g.Key, g => g.OrderBy(l => l.Ordre).Select(l => new object[] { l.IdLieu, l.Libelle }).ToList());
        json = JsonSerializer.Serialize(arbre);
        cache.Set(CleCacheLieux, json, TimeSpan.FromHours(6));
        return json;
    }
}
