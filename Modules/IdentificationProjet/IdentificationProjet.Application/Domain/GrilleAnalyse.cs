namespace IdentificationProjet.Application.Domain;

/// <summary>
/// Grille d'analyse de la fiche d'identification (DIP / Service d'analyse et de
/// programmation de l'investissement public). Une grille est générée à chaque
/// arrivée de la fiche au MPCE ; elle contient une COPIE de la fiche analysée.
/// Lecture seule : vue V_IP_GRILLE sur la table objet TB_IP_GRILLE.
/// </summary>
public class GrilleAnalyse
{
    public int IdGrille { get; set; }
    public int IdFiche { get; set; }
    public int Numero { get; set; }
    public string CodeFiche { get; set; } = "";
    public string StatutFiche { get; set; } = "";
    public string? PromoteurNom { get; set; }
    public string Statut { get; set; } = StatutGrille.EnAnalyse;

    // En-tête
    public string? Ministere { get; set; }
    public string? TitreIdee { get; set; }
    public DateTime? DateSoumissionUep { get; set; }
    public DateTime? DateReceptionDip { get; set; }

    // Brève description (copie de la fiche soumise)
    public string? Departement { get; set; }
    public string? Arrondissement { get; set; }
    public string? Commune { get; set; }
    public string? SectionCommunale { get; set; }
    public string? Localite { get; set; }
    public string? Milieu { get; set; }
    public string? Probleme { get; set; }
    public string? ObjectifGeneral { get; set; }
    public string? DonneesQuantitatives { get; set; }
    public string? DonneesQualitatives { get; set; }
    public string? MeilleureIdee { get; set; }
    public string? LegitimiteChoisie { get; set; }
    public string? PertinenceChoisie { get; set; }
    public string? ViabiliteChoisie { get; set; }
    public string? CommentairesDescription { get; set; }

    // Analyse
    public string? CommentairesGeneraux { get; set; }
    public string? Decision { get; set; }
    public string? Recommandations { get; set; }

    // Signatures
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

    /// <summary>Méthode ANOMALIE() du type objet : null si la grille peut être transmise.</summary>
    public string? Anomalie { get; set; }
    public int NbNon { get; set; }

    public List<PisteGrille> Pistes { get; set; } = new();
    public List<ReponseGrille> Reponses { get; set; } = new();
}

/// <summary>Idée possible copiée dans la grille (section 5 « Pistes de solutions »).</summary>
public class PisteGrille
{
    public int IdGrille { get; set; }
    public int Ordre { get; set; }
    public string? Intitule { get; set; }
    public string? Description { get; set; }
    public bool EstMeilleure { get; set; }
}

/// <summary>Réponse à une question « Généralités ».</summary>
public class ReponseGrille
{
    public int IdGrille { get; set; }
    public int Numero { get; set; }
    public string Question { get; set; } = "";
    public string? Reponse { get; set; }      // OUI | NON | null
    public string? Commentaire { get; set; }
}

/// <summary>Élément du référentiel géographique.</summary>
public class Lieu
{
    public int IdLieu { get; set; }
    public string Niveau { get; set; } = "";
    public string Libelle { get; set; } = "";
    public int? IdParent { get; set; }
    public int Ordre { get; set; }
}

public static class StatutGrille
{
    public const string EnAnalyse = "EN_ANALYSE";
    public const string AuChefService = "AU_CHEF_SERVICE";
    public const string AuDirecteur = "AU_DIRECTEUR";
    public const string Approuvee = "APPROUVEE";

    public static readonly IReadOnlyList<string> Tous = new[] { EnAnalyse, AuChefService, AuDirecteur, Approuvee };

    public static string Libelle(string statut) => statut switch
    {
        EnAnalyse => "En analyse (analyste)",
        AuChefService => "À valider (Chef de Service)",
        AuDirecteur => "À approuver (Directeur)",
        Approuvee => "Approuvée",
        _ => statut
    };

    public static string Couleur(string statut) => statut switch
    {
        EnAnalyse => "info",
        AuChefService or AuDirecteur => "warning",
        Approuvee => "success",
        _ => "secondary"
    };
}

public static class ActionGrille
{
    public const string AnalysteTransmettre = "ANALYSTE_TRANSMETTRE";
    public const string ChefValider = "CHEF_VALIDER";
    public const string ChefRenvoyer = "CHEF_RENVOYER";
    public const string DirecteurApprouver = "DIRECTEUR_APPROUVER";
    public const string DirecteurRenvoyer = "DIRECTEUR_RENVOYER";

    public static string Libelle(string action) => action switch
    {
        AnalysteTransmettre => "Transmettre au Chef de Service",
        ChefValider => "Valider et transmettre au Directeur",
        ChefRenvoyer or DirecteurRenvoyer => "Renvoyer à l'analyste",
        DirecteurApprouver => "Approuver (Vu et approuvé)",
        _ => action
    };

    public static bool CommentaireObligatoire(string action) => action is ChefRenvoyer or DirecteurRenvoyer;
}

/// <summary>Résultat de l'analyse (liste DECISIONS de la grille) et statut de fiche correspondant.</summary>
public static class DecisionGrille
{
    public const string Accepte = "ACCEPTE";
    public const string AReviser = "A_REVISER";
    public const string Rejete = "REJETE";

    public static readonly IReadOnlyList<string> Toutes = new[] { Accepte, AReviser, Rejete };

    /// <summary>Libellés identiques à ceux de la grille Excel.</summary>
    public static string Libelle(string? decision) => decision switch
    {
        Accepte => "Le document de l'idée de projet est accepté",
        AReviser => "Le document de l'idée de projet doit être révisé",
        Rejete => "Le document de l'idée de projet est rejeté",
        _ => ""
    };

    /// <summary>Libellés pour l'avis sur une étude.</summary>
    public static string LibelleEtude(string? decision) => decision switch
    {
        Accepte => "L'étude est acceptée",
        AReviser => "L'étude doit être révisée",
        Rejete => "L'étude est rejetée",
        _ => ""
    };

    /// <summary>Statut que prendra la fiche (ou l'étude) à l'approbation de la grille (ou de l'avis).</summary>
    public static string StatutFicheResultant(string? decision) => decision switch
    {
        Accepte => StatutFiche.ApprouveeDip,
        AReviser => StatutFiche.ComplementDip,
        Rejete => StatutFiche.RejeteeDip,
        _ => StatutFiche.TransmiseDip
    };
}
