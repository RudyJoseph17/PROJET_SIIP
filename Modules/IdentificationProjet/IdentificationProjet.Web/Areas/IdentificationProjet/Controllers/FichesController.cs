using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;
using IdentificationProjet.Web.Areas.IdentificationProjet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Controllers;

/// <summary>
/// Fiche d'Identification de l'Idée de Projet : saisie par le promoteur,
/// suivi de la progression, examen par l'UEP puis la DIP.
/// </summary>
[Area("IdentificationProjet")]
[Authorize]
public class FichesController : Controller
{
    private const long TailleRequeteMax = LimitesAnnexes.TailleMaxOctets + 5 * 1024 * 1024;
    private const string CommandeSupprimerAnnexe = "supprimer-annexe-";

    private readonly IFicheIdentificationRepository _repo;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<FichesController> _logger;

    public FichesController(IFicheIdentificationRepository repo, UserManager<ApplicationUser> users, ILogger<FichesController> logger)
    {
        _repo = repo;
        _users = users;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Liste

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        var vm = new ListeFichesViewModel
        {
            MesFiches = await _repo.ListerParPromoteurAsync(u.UserId, ct),
            EstExaminateur = u.EstUep || u.EstDip || u.EstAdmin,
        };

        var statuts = new List<string>();
        if (u.EstUep || u.EstAdmin) statuts.Add(StatutFiche.SoumiseUep);
        if (u.EstDip || u.EstAdmin) statuts.Add(StatutFiche.TransmiseDip);
        if (statuts.Count > 0)
        {
            // L'UEP ne voit que les fiches de son ministère ; DIP et Admin voient tout
            var ministere = u.EstAdmin || u.EstDip ? null : u.Ministere;
            vm.AExaminer = (await _repo.ListerParStatutsAsync(statuts, ministere, ct))
                .Where(f => ReglesAcces.ActionsExamen(f, u).Count > 0)
                .ToList();
        }
        return View(vm);
    }

    // --------------------------------------------------------------- Création

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Creer(CancellationToken ct)
    {
        var u = await UtilisateurAsync();
        var id = await _repo.CreerAsync(u.UserId, u.NomAffiche, u.Ministere, u.Courriel, ct);
        TempData["Success"] = "Nouvelle fiche d'identification créée. Complétez les sections puis soumettez-la à l'UEP.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ---------------------------------------------------------------- Saisie

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutModifier(fiche, u))
        {
            TempData["Info"] = "Cette fiche n'est pas modifiable dans son statut actuel.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(FicheEditViewModel.Depuis(fiche));
    }

    /// <summary>
    /// Enregistre toute la fiche, puis traite la commande : ajout/suppression d'annexe
    /// ou soumission. Ainsi aucune saisie n'est perdue lors d'une action secondaire.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(TailleRequeteMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = TailleRequeteMax)]
    public async Task<IActionResult> Edit(int id, FicheEditViewModel model, string? commande, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutModifier(fiche, u)) return Forbid();
        model.IdFiche = id;

        NouvelleAnnexe? annexe = null;
        if (model.NouvelleAnnexe is { Length: > 0 } fichier)
            annexe = await LireAnnexeAsync(fichier, model.DescriptionAnnexe, u, fiche.NbAnnexes, ct);

        if (!ModelState.IsValid)
            return await RevoirAsync(model, fiche);

        try
        {
            await _repo.EnregistrerAsync(model.VersEnregistrement(), ct);

            if (annexe is not null)
                await _repo.AjouterAnnexeAsync(id, annexe, ct);

            if (commande?.StartsWith(CommandeSupprimerAnnexe, StringComparison.Ordinal) == true
                && int.TryParse(commande[CommandeSupprimerAnnexe.Length..], out var idAnnexe))
                await _repo.SupprimerAnnexeAsync(id, idAnnexe, ct);

            if (commande == "soumettre")
            {
                var maj = await _repo.ObtenirAsync(id, ct);
                if (maj is not null && maj.Progression < 100)
                {
                    TempData["Warning"] = $"Brouillon enregistré, mais la fiche est complète à {maj.Progression} % : "
                                          + "complétez toutes les sections avant de la soumettre.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
                await _repo.ChangerStatutAsync(id, ActionFiche.Soumettre, null, u.UserId, u.NomAffiche, ct);
                TempData["Success"] = "La fiche a été soumise pour examen.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = annexe is not null ? "Brouillon enregistré et document annexe ajouté." : "Brouillon enregistré.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (RegleMetierException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await RevoirAsync(model, fiche);
        }
    }

    // ---------------------------------------------------------- Consultation

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutConsulter(fiche, u)) return Forbid();
        return View(new DetailsFicheViewModel { Fiche = fiche, Utilisateur = u });
    }

    [HttpGet]
    public async Task<IActionResult> Annexe(int id, int idAnnexe, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutConsulter(fiche, u)) return Forbid();

        var contenu = await _repo.LireContenuAnnexeAsync(id, idAnnexe, ct);
        return contenu is null ? NotFound() : File(contenu.Contenu, contenu.TypeMime, contenu.NomFichier);
    }

    // -------------------------------------------------------------- Workflow

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Soumettre(int id, CancellationToken ct) =>
        await ExecuterActionAsync(id, ActionFiche.Soumettre, null, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Examiner(int id, string action, string? commentaire, CancellationToken ct) =>
        await ExecuterActionAsync(id, action, commentaire, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Supprimer(int id, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutSupprimer(fiche, u)) return Forbid();
        try
        {
            await _repo.SupprimerAsync(id, ct);
            TempData["Success"] = $"La fiche {fiche.CodeFiche} a été supprimée.";
            return RedirectToAction(nameof(Index));
        }
        catch (RegleMetierException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<IActionResult> ExecuterActionAsync(int id, string action, string? commentaire, CancellationToken ct)
    {
        var (fiche, u) = await ChargerAsync(id, ct);
        if (fiche is null) return NotFound();
        if (!ReglesAcces.PeutExecuter(fiche, u, action))
        {
            TempData["Error"] = "Vous n'êtes pas autorisé à effectuer cette action sur la fiche.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ActionFiche.CommentaireObligatoire(action) && string.IsNullOrWhiteSpace(commentaire))
        {
            TempData["Error"] = "Un commentaire est obligatoire pour demander un complément ou rejeter la fiche.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _repo.ChangerStatutAsync(id, action, commentaire?.Trim(), u.UserId, u.NomAffiche, ct);
            _logger.LogInformation("Fiche {Code} : action {Action} par {User}", fiche.CodeFiche, action, u.UserId);
            TempData["Success"] = $"{ActionFiche.Libelle(action)} — fiche {fiche.CodeFiche}.";
        }
        catch (RegleMetierException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------ Utilitaires

    private async Task<(FicheIdentification? Fiche, ContexteUtilisateur Utilisateur)> ChargerAsync(int id, CancellationToken ct) =>
        (await _repo.ObtenirAsync(id, ct), await UtilisateurAsync());

    private async Task<ContexteUtilisateur> UtilisateurAsync()
    {
        var user = await _users.GetUserAsync(User);
        var nom = user is null ? null : $"{user.Prenom} {user.Nom}".Trim();
        return new ContexteUtilisateur(
            UserId: user?.Id ?? _users.GetUserId(User) ?? "",
            NomAffiche: string.IsNullOrWhiteSpace(nom) ? User.Identity?.Name ?? "" : nom,
            Ministere: user?.Ministere,
            Courriel: user?.Email,
            EstUep: User.IsInRole(RolesIdentification.Uep),
            EstDip: User.IsInRole(RolesIdentification.Dip),
            EstAdmin: User.IsInRole(RolesIdentification.Admin));
    }

    private async Task<NouvelleAnnexe?> LireAnnexeAsync(IFormFile fichier, string? description, ContexteUtilisateur u, int nbExistantes, CancellationToken ct)
    {
        const string cle = nameof(FicheEditViewModel.NouvelleAnnexe);
        var nom = Path.GetFileName(fichier.FileName);
        var ext = Path.GetExtension(nom);

        if (nbExistantes >= LimitesAnnexes.NombreMax)
            ModelState.AddModelError(cle, $"Une fiche ne peut pas avoir plus de {LimitesAnnexes.NombreMax} documents annexes.");
        else if (!LimitesAnnexes.Extensions.TryGetValue(ext, out _))
            ModelState.AddModelError(cle, $"Type de fichier non accepté ({ext}). Formats acceptés : {LimitesAnnexes.Accept}.");
        else if (fichier.Length > LimitesAnnexes.TailleMaxOctets)
            ModelState.AddModelError(cle, "Le fichier dépasse la taille maximale de 10 Mo.");
        else if (nom.Length > 255)
            ModelState.AddModelError(cle, "Le nom du fichier est trop long (255 caractères au maximum).");

        if (!ModelState.IsValid) return null;

        using var ms = new MemoryStream((int)fichier.Length);
        await fichier.CopyToAsync(ms, ct);
        return new NouvelleAnnexe(nom, LimitesAnnexes.Extensions[ext], ms.ToArray(),
                                  string.IsNullOrWhiteSpace(description) ? null : description.Trim(), u.NomAffiche);
    }

    private Task<IActionResult> RevoirAsync(FicheEditViewModel model, FicheIdentification fiche)
    {
        model.Fiche = fiche;
        return Task.FromResult<IActionResult>(View(nameof(Edit), model));
    }
}
