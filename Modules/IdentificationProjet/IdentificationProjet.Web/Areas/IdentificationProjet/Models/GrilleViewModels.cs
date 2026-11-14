using System.ComponentModel.DataAnnotations;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Models;

/// <summary>Saisie de l'analyste : Généralités (Oui/Non + commentaire), commentaires, avis.</summary>
public class GrilleEditViewModel
{
    public int IdGrille { get; set; }

    [Display(Name = "Commentaires sur la description")]
    public string? CommentairesDescription { get; set; }

    public List<ReponseLigneViewModel> Reponses { get; set; } = new();

    [Display(Name = "Commentaires généraux")]
    public string? CommentairesGeneraux { get; set; }

    [Display(Name = "Résultat de l'analyse")]
    public string? Decision { get; set; }

    [Display(Name = "Recommandations")]
    public string? Recommandations { get; set; }

    /// <summary>Grille enregistrée (copie de la fiche, signatures) — affichage seulement.</summary>
    public GrilleAnalyse? Grille { get; set; }

    public static GrilleEditViewModel Depuis(GrilleAnalyse g) => new()
    {
        IdGrille = g.IdGrille,
        CommentairesDescription = g.CommentairesDescription,
        CommentairesGeneraux = g.CommentairesGeneraux,
        Decision = g.Decision,
        Recommandations = g.Recommandations,
        Reponses = g.Reponses.Select(r => new ReponseLigneViewModel
        {
            Numero = r.Numero, Question = r.Question, Reponse = r.Reponse, Commentaire = r.Commentaire
        }).ToList(),
        Grille = g,
    };

    public SaisieGrille VersSaisie() => new()
    {
        IdGrille = IdGrille,
        CommentairesDescription = CommentairesDescription,
        CommentairesGeneraux = CommentairesGeneraux,
        Decision = DecisionGrille.Toutes.Contains(Decision ?? "") ? Decision : null,
        Recommandations = Recommandations,
        Reponses = Reponses.Select(r => new SaisieReponse(
            r.Numero,
            r.Reponse is "OUI" or "NON" ? r.Reponse : null,
            r.Reponse == "NON" ? r.Commentaire : null)).ToList(),
    };
}

public class ReponseLigneViewModel
{
    public int Numero { get; set; }
    public string? Question { get; set; }
    public string? Reponse { get; set; }

    [StringLength(2000)]
    public string? Commentaire { get; set; }
}

public class ListeGrillesViewModel
{
    public List<GrilleAnalyse> ATraiter { get; set; } = new();
    public List<GrilleAnalyse> EnCours { get; set; } = new();
    public List<GrilleAnalyse> Approuvees { get; set; } = new();
}

public class DetailsGrilleViewModel
{
    public required GrilleAnalyse Grille { get; init; }
    public required FicheIdentification Fiche { get; init; }
    public required ContexteUtilisateur Utilisateur { get; init; }
    public IReadOnlyList<string> Actions => ReglesGrille.Actions(Grille, Utilisateur);
    public bool PeutSaisir => ReglesGrille.PeutSaisir(Grille, Utilisateur);
    /// <summary>Échanges internes (renvois du Chef / du Directeur) liés aux grilles.</summary>
    public List<HistoriqueFiche> Echanges { get; init; } = new();
}
