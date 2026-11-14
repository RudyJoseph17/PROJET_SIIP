namespace IdentificationProjet.Application.Domain;

/// <summary>Phases du cycle d'un projet, après l'identification.</summary>
public static class PhaseProjet
{
    public const string Identification = "IDENTIFICATION";
    public const string Prefaisabilite = "PREFAISABILITE";
    public const string Faisabilite = "FAISABILITE";
    public const string EtudesProjet = "ETUDES_PROJET";

    /// <summary>Les trois phases d'études, dans l'ordre.</summary>
    public static readonly IReadOnlyList<string> Etudes = new[] { Prefaisabilite, Faisabilite, EtudesProjet };

    public static string Libelle(string? phase) => phase switch
    {
        Identification or null => "Identification",
        Prefaisabilite => "Étude de préfaisabilité",
        Faisabilite => "Étude de faisabilité",
        EtudesProjet => "Études de projet",
        _ => phase
    };

    /// <summary>Libellé court pour les listes.</summary>
    public static string LibelleCourt(string? phase) => phase switch
    {
        Identification or null => "Identification",
        Prefaisabilite => "Préfaisabilité",
        Faisabilite => "Faisabilité",
        EtudesProjet => "Études de projet",
        _ => phase
    };

    /// <summary>Statut du projet affiché dans les listes : phase courante + statut dans la phase.</summary>
    public static string LibelleStatutProjet(string? phase, string statut, bool projetValide) =>
        projetValide ? "Projet validé" : $"{LibelleCourt(phase)} — {StatutFiche.Libelle(statut)}";

    public static string CouleurStatutProjet(string statut, bool projetValide) =>
        projetValide ? "success" : statut == StatutFiche.ApprouveeDip ? "info" : StatutFiche.Couleur(statut);
}

/// <summary>Étude d'une phase (préfaisabilité, faisabilité, études de projet). Vue V_IP_ETUDE.</summary>
public class EtudeProjet
{
    public int IdEtude { get; set; }
    public int IdFiche { get; set; }
    public string CodeFiche { get; set; } = "";
    public string? TitreProjet { get; set; }
    public string? Ministere { get; set; }
    public string? NomOrganisation { get; set; }
    public string PromoteurId { get; set; } = "";
    public string? PromoteurNom { get; set; }
    public string Phase { get; set; } = PhaseProjet.Prefaisabilite;
    public int OrdrePhase { get; set; }
    public string Statut { get; set; } = StatutFiche.Brouillon;
    public string? Description { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public DateTime? DateSoumission { get; set; }
    public DateTime? DateValidation { get; set; }
    public bool EstModifiable { get; set; }
    public int NbDocuments { get; set; }

    public List<DocumentEtude> Documents { get; set; } = new();
    public List<AvisEtude> Avis { get; set; } = new();
}

public class DocumentEtude
{
    public int IdDocument { get; set; }
    public int IdEtude { get; set; }
    public string NomFichier { get; set; } = "";
    public string? TypeMime { get; set; }
    public long Taille { get; set; }
    public string? Description { get; set; }
    public DateTime DateAjout { get; set; }
    public string? AjoutePar { get; set; }
}

/// <summary>Avis du MPCE sur une étude (circuit Analyste → Chef de Service → Directeur). Vue V_IP_AVIS.</summary>
public class AvisEtude
{
    public int IdAvis { get; set; }
    public int IdEtude { get; set; }
    public int IdFiche { get; set; }
    public string CodeFiche { get; set; } = "";
    public string? TitreProjet { get; set; }
    public string? NomOrganisation { get; set; }
    public string Phase { get; set; } = "";
    public string StatutEtude { get; set; } = "";
    public int Numero { get; set; }
    public string Statut { get; set; } = StatutGrille.EnAnalyse;   // mêmes statuts que la grille
    public string? Decision { get; set; }
    public string? Commentaire { get; set; }
    public string? Recommandations { get; set; }
    public string? AnalysteId { get; set; }
    public string? AnalysteNom { get; set; }
    public DateTime? DateAnalyste { get; set; }
    public string? ChefId { get; set; }
    public string? ChefNom { get; set; }
    public DateTime? DateChef { get; set; }
    public string? DirecteurId { get; set; }
    public string? DirecteurNom { get; set; }
    public DateTime? DateDirecteur { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public string? Anomalie { get; set; }
}
