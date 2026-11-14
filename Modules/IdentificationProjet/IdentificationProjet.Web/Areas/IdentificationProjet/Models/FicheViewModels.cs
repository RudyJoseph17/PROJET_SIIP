using System.ComponentModel.DataAnnotations;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using IdentificationProjet.Application.Workflow;
using Microsoft.AspNetCore.Http;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Models;

/// <summary>Formulaire de saisie de la fiche (brouillon : aucun champ n'est obligatoire à l'enregistrement).</summary>
public class FicheEditViewModel
{
    public int IdFiche { get; set; }

    // 1.1 Contact principal
    [Display(Name = "Nom de l'organisation"), StringLength(200)]
    public string? NomOrganisation { get; set; }

    [Display(Name = "Nom de la personne contact"), StringLength(150)]
    public string? NomContact { get; set; }

    [Display(Name = "Téléphone"), StringLength(30), Phone]
    public string? Telephone { get; set; }

    [Display(Name = "Adresse électronique"), StringLength(150), EmailAddress]
    public string? Courriel { get; set; }

    // 1.3
    [Display(Name = "Description du problème à résoudre ou du besoin à satisfaire")]
    public string? DescriptionProbleme { get; set; }

    // 2.1 / 2.2
    [Display(Name = "Présentation des données pour illustrer l'existence du problème/besoin")]
    public string? DonneesProbleme { get; set; }

    [Display(Name = "Conséquences probables si le problème/besoin n'est pas traité")]
    public string? Consequences { get; set; }

    // 3.1 – 3.4
    public List<IdeeLigneViewModel> Idees { get; set; } = new();

    // 3.5
    public int? IndexMeilleureIdee { get; set; }

    [Display(Name = "Justification du choix de la meilleure idée")]
    public string? JustificationChoix { get; set; }

    // 1.2 (affiché après la détermination de la meilleure idée)
    [Display(Name = "Titre proposé du projet"), StringLength(500)]
    public string? TitreProjet { get; set; }

    // 4.1
    [Display(Name = "Premières conclusions et recommandations pour les prochaines étapes")]
    public string? Conclusions { get; set; }

    // 5. Nouveau document annexe (facultatif)
    public IFormFile? NouvelleAnnexe { get; set; }

    [Display(Name = "Description du document"), StringLength(300)]
    public string? DescriptionAnnexe { get; set; }

    /// <summary>Fiche telle qu'enregistrée (progression, annexes, historique) — affichage seulement.</summary>
    public FicheIdentification? Fiche { get; set; }

    public static FicheEditViewModel Depuis(FicheIdentification f) => new()
    {
        IdFiche = f.IdFiche,
        NomOrganisation = f.NomOrganisation,
        NomContact = f.NomContact,
        Telephone = f.Telephone,
        Courriel = f.Courriel,
        DescriptionProbleme = f.DescriptionProbleme,
        DonneesProbleme = f.DonneesProbleme,
        Consequences = f.Consequences,
        Idees = f.Idees.Select(i => new IdeeLigneViewModel
        {
            IdIdee = i.IdIdee,
            Intitule = i.Intitule,
            Description = i.Description,
            Legitimite = i.Legitimite,
            Pertinence = i.Pertinence,
            Viabilite = i.Viabilite,
        }).ToList(),
        IndexMeilleureIdee = f.Idees.FindIndex(i => i.EstMeilleure) is var idx and >= 0 ? idx : null,
        JustificationChoix = f.JustificationChoix,
        TitreProjet = f.TitreProjet,
        Conclusions = f.Conclusions,
        Fiche = f,
    };

    /// <summary>Convertit la saisie ; les lignes d'idées entièrement vides sont ignorées.</summary>
    public EnregistrementFiche VersEnregistrement()
    {
        var lignes = Idees.Select((idee, index) => (idee, index)).Where(x => !x.idee.EstVide).ToList();
        int? meilleure = null;
        if (IndexMeilleureIdee is int choisi)
        {
            var pos = lignes.FindIndex(x => x.index == choisi);
            if (pos >= 0) meilleure = pos;
        }

        return new EnregistrementFiche
        {
            IdFiche = IdFiche,
            NomOrganisation = NomOrganisation,
            NomContact = NomContact,
            Telephone = Telephone,
            Courriel = Courriel,
            DescriptionProbleme = DescriptionProbleme,
            DonneesProbleme = DonneesProbleme,
            Consequences = Consequences,
            Conclusions = Conclusions,
            Idees = lignes.Select(x => new SaisieIdee
            {
                IdIdee = x.idee.IdIdee,
                Intitule = x.idee.Intitule,
                Description = x.idee.Description,
                Legitimite = x.idee.Legitimite,
                Pertinence = x.idee.Pertinence,
                Viabilite = x.idee.Viabilite,
            }).ToList(),
            IndexMeilleureIdee = meilleure,
            JustificationChoix = meilleure.HasValue ? JustificationChoix : null,
            TitreProjet = meilleure.HasValue ? TitreProjet : null,
        };
    }
}

/// <summary>Une ligne du tableau des idées possibles (3.1 Idée | 3.2 Légitimité | 3.3 Pertinence | 3.4 Viabilité).</summary>
public class IdeeLigneViewModel
{
    public int? IdIdee { get; set; }

    [StringLength(500)]
    public string? Intitule { get; set; }
    public string? Description { get; set; }
    public string? Legitimite { get; set; }
    public string? Pertinence { get; set; }
    public string? Viabilite { get; set; }

    public bool EstVide =>
        string.IsNullOrWhiteSpace(Intitule) && string.IsNullOrWhiteSpace(Description)
        && string.IsNullOrWhiteSpace(Legitimite) && string.IsNullOrWhiteSpace(Pertinence)
        && string.IsNullOrWhiteSpace(Viabilite);
}

public class ListeFichesViewModel
{
    public List<FicheIdentification> MesFiches { get; set; } = new();
    public List<FicheIdentification> AExaminer { get; set; } = new();
    public bool EstExaminateur { get; set; }
}

public class DetailsFicheViewModel
{
    public required FicheIdentification Fiche { get; init; }
    public required ContexteUtilisateur Utilisateur { get; init; }
    public bool PeutModifier => ReglesAcces.PeutModifier(Fiche, Utilisateur);
    public bool PeutSoumettre => ReglesAcces.PeutSoumettre(Fiche, Utilisateur);
    public bool PeutSupprimer => ReglesAcces.PeutSupprimer(Fiche, Utilisateur);
    public IReadOnlyList<string> ActionsExamen => ReglesAcces.ActionsExamen(Fiche, Utilisateur);
}
