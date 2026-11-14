using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;
using IdentificationProjet.Web.Areas.IdentificationProjet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Controllers;

/// <summary>
/// Avis du MPCE sur une étude (préfaisabilité, faisabilité, études de projet) :
/// rédigé par l'Analyste, validé par le Chef de Service, approuvé par le Directeur.
/// L'avis est généré par la base à chaque arrivée d'une étude au MPCE.
/// </summary>
public class AvisController : ModuleControllerBase
{
    private readonly IEtudeRepository _etudes;
    private readonly IFicheIdentificationRepository _fiches;
    private readonly ILogger<AvisController> _logger;

    public AvisController(IEtudeRepository etudes, IFicheIdentificationRepository fiches,
                          UserManager<ApplicationUser> users, ILogger<AvisController> logger) : base(users)
    {
        _etudes = etudes;
        _fiches = fiches;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        if (!u.EstMpce) return Forbid();

        var tous = await _etudes.ListerAvisAsync(null, ct);
        var vm = new ListeAvisViewModel
        {
            ATraiter = tous.Where(a => ReglesGrille.Actions(a, u).Count > 0).ToList(),
            Approuves = tous.Where(a => a.Statut == StatutGrille.Approuvee).OrderByDescending(a => a.DateDirecteur).ToList(),
        };
        vm.EnCours = tous.Where(a => a.Statut != StatutGrille.Approuvee && !vm.ATraiter.Contains(a)).ToList();
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var (a, e, u) = await ChargerAsync(id, ct);
        if (a is null || e is null) return NotFound();
        if (!ReglesEtude.PeutConsulterAvis(a, e, u)) return Forbid();

        var echanges = new List<HistoriqueFiche>();
        if (u.EstMpce)
        {
            var fiche = await _fiches.ObtenirAsync(e.IdFiche, ct);
            echanges = (fiche?.Historique ?? new())
                .Where(h => h.Phase == e.Phase
                            && (h.Action == "AVIS_" + ActionGrille.ChefRenvoyer || h.Action == "AVIS_" + ActionGrille.DirecteurRenvoyer)
                            && h.DateAction >= a.DateCreation
                            && (a.DateDirecteur is null || h.DateAction <= a.DateDirecteur))
                .ToList();
        }
        return View(new DetailsAvisViewModel { Avis = a, Etude = e, Utilisateur = u, Echanges = echanges });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var (a, e, u) = await ChargerAsync(id, ct);
        if (a is null || e is null) return NotFound();
        if (!ReglesGrille.PeutSaisir(a, u))
        {
            TempData["Info"] = "Cet avis n'est pas modifiable : il a été transmis pour validation.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new AvisEditViewModel
        {
            IdAvis = id, Decision = a.Decision, Commentaire = a.Commentaire,
            Recommandations = a.Recommandations, Avis = a, Etude = e,
        });
    }

    /// <summary>Enregistre l'avis ; avec commande = "transmettre", le transmet au Chef de Service.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AvisEditViewModel model, string? commande, CancellationToken ct)
    {
        var (a, e, u) = await ChargerAsync(id, ct);
        if (a is null || e is null) return NotFound();
        if (!ReglesGrille.PeutSaisir(a, u)) return Forbid();

        var decision = DecisionGrille.Toutes.Contains(model.Decision ?? "") ? model.Decision : null;
        try
        {
            await _etudes.EnregistrerAvisAsync(id, decision, model.Commentaire, model.Recommandations, ct);
            if (commande == "transmettre")
            {
                await _etudes.ExecuterActionAvisAsync(id, ActionGrille.AnalysteTransmettre, null, u.UserId, u.NomAffiche, ct);
                TempData["Success"] = $"Avis transmis au Chef de Service ({PhaseProjet.Libelle(e.Phase)}, {e.CodeFiche}).";
                return RedirectToAction(nameof(Details), new { id });
            }
            TempData["Success"] = "Avis enregistré.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (RegleMetierException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Avis = await _etudes.ObtenirAvisAsync(id, ct) ?? a;
            model.Etude = e;
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Traiter(int id, string operation, string? commentaire, CancellationToken ct)
    {
        var (a, e, u) = await ChargerAsync(id, ct);
        if (a is null || e is null) return NotFound();
        if (operation == ActionGrille.AnalysteTransmettre || !ReglesGrille.Actions(a, u).Contains(operation))
        {
            TempData["Error"] = "Vous n'êtes pas autorisé à effectuer cette action sur l'avis.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ActionGrille.CommentaireObligatoire(operation) && string.IsNullOrWhiteSpace(commentaire))
        {
            TempData["Error"] = "Un commentaire est obligatoire pour renvoyer l'avis à l'analyste.";
            return RedirectToAction(nameof(Details), new { id });
        }
        try
        {
            await _etudes.ExecuterActionAvisAsync(id, operation, commentaire?.Trim(), u.UserId, u.NomAffiche, ct);
            _logger.LogInformation("Avis {Id} ({Code}, {Phase}) : {Operation} par {User}", id, e.CodeFiche, e.Phase, operation, u.UserId);
            TempData["Success"] = operation == ActionGrille.DirecteurApprouver
                ? $"Avis approuvé. {PhaseProjet.Libelle(e.Phase)} : « {StatutFiche.Libelle(DecisionGrille.StatutFicheResultant(a.Decision))} »."
                  + (a.Decision == DecisionGrille.Accepte
                        ? (e.Phase == PhaseProjet.EtudesProjet ? " Le projet est validé et inscrit à la Banque de projets." : " La phase suivante est ouverte.")
                        : "")
                : $"{ActionGrille.Libelle(operation)} — avis n° {a.Numero} ({e.CodeFiche}).";
        }
        catch (RegleMetierException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<(AvisEtude? Avis, EtudeProjet? Etude, ContexteUtilisateur Utilisateur)> ChargerAsync(int id, CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        var a = await _etudes.ObtenirAvisAsync(id, ct);
        var e = a is null ? null : await _etudes.ObtenirAsync(a.IdEtude, ct);
        return (a, e, u);
    }
}
