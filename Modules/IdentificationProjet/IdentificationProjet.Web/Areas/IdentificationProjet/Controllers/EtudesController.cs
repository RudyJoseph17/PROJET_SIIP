using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;
using IdentificationProjet.Web.Areas.IdentificationProjet.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Domain.Entities;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Controllers;

/// <summary>
/// Phases d'études d'un projet validé : préfaisabilité, faisabilité, études de projet.
/// Le promoteur joint ses documents et soumet ; l'UEP examine ; le MPCE rend un avis (AvisController).
/// </summary>
public class EtudesController : ModuleControllerBase
{
    private const long TailleRequeteMax = 6 * LimitesDocumentsEtude.TailleMaxOctets;
    private const string CommandeSupprimerDocument = "supprimer-document-";

    private readonly IEtudeRepository _etudes;
    private readonly IFicheIdentificationRepository _fiches;
    private readonly ILogger<EtudesController> _logger;

    public EtudesController(IEtudeRepository etudes, IFicheIdentificationRepository fiches,
                            UserManager<ApplicationUser> users, ILogger<EtudesController> logger) : base(users)
    {
        _etudes = etudes;
        _fiches = fiches;
        _logger = logger;
    }

    // ---------------------------------------------------------- Consultation

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var (e, u) = await ChargerAsync(id, ct);
        if (e is null) return NotFound();
        if (!ReglesEtude.PeutConsulter(e, u)) return Forbid();

        var fiche = await _fiches.ObtenirAsync(e.IdFiche, ct);
        var historique = (fiche?.Historique ?? new())
            .Where(h => h.Phase == e.Phase
                        && (ReglesAcces.VoitEtapesInternesMpce(u) || !ActionFiche.EstInterneMpce(h.Action)))
            .ToList();
        return View(new DetailsEtudeViewModel { Etude = e, Utilisateur = u, Historique = historique });
    }

    public async Task<IActionResult> Document(int id, int idDocument, CancellationToken ct)
    {
        var (e, u) = await ChargerAsync(id, ct);
        if (e is null) return NotFound();
        if (!ReglesEtude.PeutConsulter(e, u)) return Forbid();
        var doc = await _etudes.LireDocumentAsync(id, idDocument, ct);
        return doc is null ? NotFound() : File(doc.Contenu, doc.TypeMime, doc.NomFichier);
    }

    // ---------------------------------------------------------------- Saisie

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var (e, u) = await ChargerAsync(id, ct);
        if (e is null) return NotFound();
        if (!ReglesEtude.PeutModifier(e, u))
        {
            TempData["Info"] = "Cette étude n'est pas modifiable dans son statut actuel.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new EtudeEditViewModel { IdEtude = id, Description = e.Description, Etude = e });
    }

    /// <summary>Enregistre la note et les documents ; commande = soumettre / supprimer-document-{id}.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(TailleRequeteMax)]
    [RequestFormLimits(MultipartBodyLengthLimit = TailleRequeteMax)]
    public async Task<IActionResult> Edit(int id, EtudeEditViewModel model, string? commande, CancellationToken ct)
    {
        var (e, u) = await ChargerAsync(id, ct);
        if (e is null) return NotFound();
        if (!ReglesEtude.PeutModifier(e, u)) return Forbid();
        model.IdEtude = id;

        var nouveaux = new List<NouvelleAnnexe>();
        foreach (var f in model.NouveauxDocuments.Where(f => f.Length > 0))
        {
            var doc = await LireDocumentAsync(f, model.DescriptionDocuments, u, ct);
            if (doc is not null) nouveaux.Add(doc);
        }
        if (e.NbDocuments + nouveaux.Count > LimitesDocumentsEtude.NombreMax)
            ModelState.AddModelError(nameof(model.NouveauxDocuments),
                $"Une étude ne peut pas avoir plus de {LimitesDocumentsEtude.NombreMax} pièces jointes.");
        if (!ModelState.IsValid)
        {
            model.Etude = e;
            return View(model);
        }

        try
        {
            await _etudes.MajAsync(id, model.Description, ct);
            foreach (var doc in nouveaux) await _etudes.AjouterDocumentAsync(id, doc, ct);

            if (commande?.StartsWith(CommandeSupprimerDocument, StringComparison.Ordinal) == true
                && int.TryParse(commande[CommandeSupprimerDocument.Length..], out var idDocument))
                await _etudes.SupprimerDocumentAsync(id, idDocument, ct);

            if (commande == "soumettre")
            {
                await _etudes.ChangerStatutAsync(id, ActionFiche.Soumettre, null, u.UserId, u.NomAffiche, ct);
                TempData["Success"] = $"{PhaseProjet.Libelle(e.Phase)} : étude soumise pour examen.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = nouveaux.Count > 0 ? $"Étude enregistrée ({nouveaux.Count} document(s) ajouté(s))." : "Étude enregistrée.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (RegleMetierException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Etude = await _etudes.ObtenirAsync(id, ct) ?? e;
            return View(model);
        }
    }

    // -------------------------------------------------------------- Workflow

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Soumettre(int id, CancellationToken ct) => ExecuterAsync(id, ActionFiche.Soumettre, null, ct);

    /// <summary>Examen par l'UEP (approuver et transmettre au MPCE, demander un complément, rejeter).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Examiner(int id, string operation, string? commentaire, CancellationToken ct) =>
        ExecuterAsync(id, operation, commentaire, ct);

    private async Task<IActionResult> ExecuterAsync(int id, string operation, string? commentaire, CancellationToken ct)
    {
        var (e, u) = await ChargerAsync(id, ct);
        if (e is null) return NotFound();
        if (!ReglesEtude.PeutExecuter(e, u, operation))
        {
            TempData["Error"] = "Vous n'êtes pas autorisé à effectuer cette action sur l'étude.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ActionFiche.CommentaireObligatoire(operation) && string.IsNullOrWhiteSpace(commentaire))
        {
            TempData["Error"] = "Un commentaire est obligatoire pour demander un complément ou rejeter l'étude.";
            return RedirectToAction(nameof(Details), new { id });
        }
        try
        {
            await _etudes.ChangerStatutAsync(id, operation, commentaire?.Trim(), u.UserId, u.NomAffiche, ct);
            _logger.LogInformation("Étude {Id} ({Phase}) : {Operation} par {User}", id, e.Phase, operation, u.UserId);
            TempData["Success"] = $"{ActionFiche.Libelle(operation)} — {PhaseProjet.Libelle(e.Phase)} ({e.CodeFiche}).";
        }
        catch (RegleMetierException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------ Utilitaires

    private async Task<(EtudeProjet? Etude, ContexteUtilisateur Utilisateur)> ChargerAsync(int id, CancellationToken ct) =>
        (await _etudes.ObtenirAsync(id, ct), await UtilisateurAsync());

    private async Task<NouvelleAnnexe?> LireDocumentAsync(IFormFile fichier, string? description, ContexteUtilisateur u, CancellationToken ct)
    {
        const string cle = nameof(EtudeEditViewModel.NouveauxDocuments);
        var nom = Path.GetFileName(fichier.FileName);
        var ext = Path.GetExtension(nom);
        if (!LimitesAnnexes.Extensions.TryGetValue(ext, out var mime))
        {
            ModelState.AddModelError(cle, $"« {nom} » : type de fichier non accepté. Formats acceptés : {LimitesAnnexes.Accept}.");
            return null;
        }
        if (fichier.Length > LimitesDocumentsEtude.TailleMaxOctets)
        {
            ModelState.AddModelError(cle, $"« {nom} » dépasse la taille maximale de 20 Mo.");
            return null;
        }
        if (nom.Length > 255)
        {
            ModelState.AddModelError(cle, $"Le nom du fichier « {nom[..40]}… » est trop long.");
            return null;
        }
        using var ms = new MemoryStream((int)fichier.Length);
        await fichier.CopyToAsync(ms, ct);
        return new NouvelleAnnexe(nom, mime, ms.ToArray(),
                                  string.IsNullOrWhiteSpace(description) ? null : description.Trim(), u.NomAffiche);
    }
}
