using System.ComponentModel.DataAnnotations;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;
using Microsoft.AspNetCore.Http;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Models;

public class DetailsEtudeViewModel
{
    public required EtudeProjet Etude { get; init; }
    public required ContexteUtilisateur Utilisateur { get; init; }
    /// <summary>Historique de la phase (sans les échanges internes du MPCE pour le promoteur).</summary>
    public List<HistoriqueFiche> Historique { get; init; } = new();
    public bool PeutModifier => ReglesEtude.PeutModifier(Etude, Utilisateur);
    public bool PeutSoumettre => ReglesEtude.PeutSoumettre(Etude, Utilisateur);
    public IReadOnlyList<string> ActionsExamen => ReglesEtude.ActionsExamen(Etude, Utilisateur);
    public IEnumerable<AvisEtude> AvisVisibles => Etude.Avis.Where(a => ReglesEtude.PeutConsulterAvis(a, Etude, Utilisateur));
}

public class EtudeEditViewModel
{
    public int IdEtude { get; set; }

    [Display(Name = "Note de présentation")]
    public string? Description { get; set; }

    /// <summary>Pièces jointes à ajouter (plusieurs à la fois).</summary>
    public List<IFormFile> NouveauxDocuments { get; set; } = new();

    [Display(Name = "Description des documents ajoutés"), StringLength(300)]
    public string? DescriptionDocuments { get; set; }

    public EtudeProjet? Etude { get; set; }
}

public class AvisEditViewModel
{
    public int IdAvis { get; set; }

    [Display(Name = "Résultat de l'analyse")]
    public string? Decision { get; set; }

    [Display(Name = "Avis de l'analyse")]
    public string? Commentaire { get; set; }

    [Display(Name = "Recommandations")]
    public string? Recommandations { get; set; }

    public AvisEtude? Avis { get; set; }
    public EtudeProjet? Etude { get; set; }
}

public class ListeAvisViewModel
{
    public List<AvisEtude> ATraiter { get; set; } = new();
    public List<AvisEtude> EnCours { get; set; } = new();
    public List<AvisEtude> Approuves { get; set; } = new();
}

public class DetailsAvisViewModel
{
    public required AvisEtude Avis { get; init; }
    public required EtudeProjet Etude { get; init; }
    public required ContexteUtilisateur Utilisateur { get; init; }
    public List<HistoriqueFiche> Echanges { get; init; } = new();
    public IReadOnlyList<string> Actions => ReglesGrille.Actions(Avis, Utilisateur);
    public bool PeutSaisir => ReglesGrille.PeutSaisir(Avis, Utilisateur);
}
