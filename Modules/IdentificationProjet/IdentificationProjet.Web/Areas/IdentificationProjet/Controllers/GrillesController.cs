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
/// Grille d'analyse de la fiche d'identification (MPCE / DIP) :
/// saisie par l'analyste, validation par le Chef de Service, approbation par le Directeur.
/// La grille est générée par la base à chaque arrivée d'une fiche au MPCE.
/// </summary>
public class GrillesController : ModuleControllerBase
{
    private readonly IGrilleAnalyseRepository _grilles;
    private readonly IFicheIdentificationRepository _fiches;
    private readonly IGrilleExcelExporter _excel;
    private readonly ILogger<GrillesController> _logger;

    public GrillesController(IGrilleAnalyseRepository grilles, IFicheIdentificationRepository fiches,
                             IGrilleExcelExporter excel, UserManager<ApplicationUser> users,
                             ILogger<GrillesController> logger) : base(users)
    {
        _grilles = grilles;
        _fiches = fiches;
        _excel = excel;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Liste

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        if (!u.EstMpce) return Forbid();

        var toutes = await _grilles.ListerAsync(null, ct);
        var vm = new ListeGrillesViewModel
        {
            ATraiter = toutes.Where(g => ReglesGrille.Actions(g, u).Count > 0).ToList(),
            Approuvees = toutes.Where(g => g.Statut == StatutGrille.Approuvee)
                               .OrderByDescending(g => g.DateDirecteur).ToList(),
        };
        vm.EnCours = toutes.Where(g => g.Statut != StatutGrille.Approuvee && !vm.ATraiter.Contains(g)).ToList();
        return View(vm);
    }

    // ---------------------------------------------------------- Consultation

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var (g, f, u) = await ChargerAsync(id, ct);
        if (g is null || f is null) return NotFound();
        if (!ReglesGrille.PeutConsulter(g, f, u)) return Forbid();

        return View(new DetailsGrilleViewModel
        {
            Grille = g, Fiche = f, Utilisateur = u,
            // Renvois du Chef de Service / du Directeur pendant la vie de CETTE grille
            Echanges = u.EstMpce
                ? f.Historique.Where(h => (h.Action == "GRILLE_" + ActionGrille.ChefRenvoyer
                                           || h.Action == "GRILLE_" + ActionGrille.DirecteurRenvoyer)
                                          && h.DateAction >= g.DateCreation
                                          && (g.DateDirecteur is null || h.DateAction <= g.DateDirecteur)).ToList()
                : new(),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Excel(int id, CancellationToken ct)
    {
        var (g, f, u) = await ChargerAsync(id, ct);
        if (g is null || f is null) return NotFound();
        if (!ReglesGrille.PeutConsulter(g, f, u)) return Forbid();

        var contenu = _excel.Exporter(g);
        return File(contenu, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Grille_analyse_{g.CodeFiche}_n{g.Numero}.xlsx");
    }

    // ---------------------------------------------------------------- Saisie

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var (g, f, u) = await ChargerAsync(id, ct);
        if (g is null || f is null) return NotFound();
        if (!ReglesGrille.PeutSaisir(g, u))
        {
            TempData["Info"] = "Cette grille n'est pas modifiable : elle a été transmise pour validation.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(GrilleEditViewModel.Depuis(g));
    }

    /// <summary>Enregistre la grille ; avec commande = "transmettre", la transmet au Chef de Service.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GrilleEditViewModel model, string? commande, CancellationToken ct)
    {
        var (g, f, u) = await ChargerAsync(id, ct);
        if (g is null || f is null) return NotFound();
        if (!ReglesGrille.PeutSaisir(g, u)) return Forbid();
        model.IdGrille = id;

        if (!ModelState.IsValid) return Revoir(model, g);

        try
        {
            await _grilles.EnregistrerAsync(model.VersSaisie(), ct);

            if (commande == "transmettre")
            {
                await _grilles.ExecuterActionAsync(id, ActionGrille.AnalysteTransmettre, null, u.UserId, u.NomAffiche, ct);
                TempData["Success"] = $"Grille n° {g.Numero} de la fiche {g.CodeFiche} transmise au Chef de Service.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = "Grille enregistrée.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (RegleMetierException ex)
        {
            // L'enregistrement a réussi si l'erreur vient de la transmission : on recharge
            ModelState.AddModelError(string.Empty, ex.Message);
            var maj = await _grilles.ObtenirAsync(id, ct);
            return Revoir(model, maj ?? g);
        }
    }

    // ------------------------------------------------- Validation / approbation

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Traiter(int id, string operation, string? commentaire, CancellationToken ct)
    {
        var (g, f, u) = await ChargerAsync(id, ct);
        if (g is null || f is null) return NotFound();

        if (operation == ActionGrille.AnalysteTransmettre || !ReglesGrille.Actions(g, u).Contains(operation))
        {
            TempData["Error"] = "Vous n'êtes pas autorisé à effectuer cette action sur la grille.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ActionGrille.CommentaireObligatoire(operation) && string.IsNullOrWhiteSpace(commentaire))
        {
            TempData["Error"] = "Un commentaire est obligatoire pour renvoyer la grille à l'analyste.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _grilles.ExecuterActionAsync(id, operation, commentaire?.Trim(), u.UserId, u.NomAffiche, ct);
            _logger.LogInformation("Grille {Id} ({Code}) : {Operation} par {User}", id, g.CodeFiche, operation, u.UserId);
            TempData["Success"] = operation == ActionGrille.DirecteurApprouver
                ? $"Grille approuvée. La fiche {g.CodeFiche} passe au statut « {StatutFiche.Libelle(DecisionGrille.StatutFicheResultant(g.Decision))} »."
                : $"{ActionGrille.Libelle(operation)} — grille n° {g.Numero} de la fiche {g.CodeFiche}.";
        }
        catch (RegleMetierException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------ Utilitaires

    private async Task<(GrilleAnalyse? Grille, FicheIdentification? Fiche, ContexteUtilisateur Utilisateur)> ChargerAsync(int id, CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        var g = await _grilles.ObtenirAsync(id, ct);
        var f = g is null ? null : await _fiches.ObtenirAsync(g.IdFiche, ct);
        return (g, f, u);
    }

    private IActionResult Revoir(GrilleEditViewModel model, GrilleAnalyse g)
    {
        model.Grille = g;
        // Les libellés des questions ne sont pas renvoyés par le formulaire
        foreach (var r in model.Reponses)
            r.Question ??= g.Reponses.FirstOrDefault(x => x.Numero == r.Numero)?.Question;
        return View(nameof(Edit), model);
    }
}
