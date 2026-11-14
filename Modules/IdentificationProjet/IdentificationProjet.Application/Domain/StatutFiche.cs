namespace IdentificationProjet.Application.Domain;

/// <summary>
/// Statuts de la fiche (identiques à la contrainte CK_IP_FICHE_STATUT).
/// Circuit : Manuel de procédures §3.E — promoteur → UEP → DIP.
/// </summary>
public static class StatutFiche
{
    public const string Brouillon = "BROUILLON";
    public const string SoumiseUep = "SOUMISE_UEP";
    public const string ComplementUep = "COMPLEMENT_UEP";
    public const string RejeteeUep = "REJETEE_UEP";
    public const string TransmiseDip = "TRANSMISE_DIP";
    public const string ComplementDip = "COMPLEMENT_DIP";
    public const string RejeteeDip = "REJETEE_DIP";
    public const string ApprouveeDip = "APPROUVEE_DIP";

    public static readonly IReadOnlyList<string> Tous = new[]
    {
        Brouillon, SoumiseUep, ComplementUep, RejeteeUep,
        TransmiseDip, ComplementDip, RejeteeDip, ApprouveeDip
    };

    public static string Libelle(string statut) => statut switch
    {
        Brouillon => "Brouillon",
        SoumiseUep => "Soumise à l'UEP",
        ComplementUep => "Complément demandé (UEP)",
        RejeteeUep => "Rejetée par l'UEP",
        TransmiseDip => "Transmise à la DIP",
        ComplementDip => "Complément demandé (DIP)",
        RejeteeDip => "Rejetée par la DIP",
        ApprouveeDip => "Approuvée — Avant-Projet",
        _ => statut
    };

    /// <summary>Classe Bootstrap 4 du badge.</summary>
    public static string Couleur(string statut) => statut switch
    {
        Brouillon => "secondary",
        SoumiseUep or TransmiseDip => "primary",
        ComplementUep or ComplementDip => "warning",
        RejeteeUep or RejeteeDip => "danger",
        ApprouveeDip => "success",
        _ => "light"
    };

    public static bool EstRejetee(string statut) => statut is RejeteeUep or RejeteeDip;

    /// <summary>
    /// Position dans le circuit (0 Rédaction, 1 Examen UEP, 2 Examen DIP, 3 Approuvée)
    /// pour l'affichage de la frise de suivi.
    /// </summary>
    public static int Jalon(string statut) => statut switch
    {
        Brouillon or ComplementUep => 0,
        SoumiseUep or RejeteeUep => 1,
        TransmiseDip or ComplementDip or RejeteeDip => 2,
        ApprouveeDip => 3,
        _ => 0
    };

    public static readonly IReadOnlyList<string> Jalons = new[]
    {
        "Rédaction (promoteur)", "Examen UEP", "Examen DIP", "Approuvée"
    };
}

/// <summary>Actions du circuit (paramètre P_ACTION de PKG_IP_IDENTIFICATION.CHANGER_STATUT).</summary>
public static class ActionFiche
{
    public const string Soumettre = "SOUMETTRE";
    public const string UepApprouver = "UEP_APPROUVER";
    public const string UepComplement = "UEP_COMPLEMENT";
    public const string UepRejeter = "UEP_REJETER";
    public const string DipApprouver = "DIP_APPROUVER";
    public const string DipComplement = "DIP_COMPLEMENT";
    public const string DipRejeter = "DIP_REJETER";

    public static string Libelle(string action) => action switch
    {
        "CREATION" => "Création de la fiche",
        Soumettre => "Soumission",
        UepApprouver => "Approuvée par l'UEP et transmise à la DIP",
        UepComplement => "Complément demandé par l'UEP",
        UepRejeter => "Rejetée par l'UEP",
        DipApprouver => "Approuvée par la DIP (passage à l'Avant-Projet)",
        DipComplement => "Complément demandé par la DIP",
        DipRejeter => "Rejetée par la DIP",
        _ => action
    };

    public static bool CommentaireObligatoire(string action) =>
        action is UepComplement or UepRejeter or DipComplement or DipRejeter;
}

/// <summary>Les 5 étapes de rédaction mesurées par la progression.</summary>
public static class EtapesFiche
{
    public static readonly IReadOnlyList<(int Numero, string Libelle, string Ancre)> Liste = new[]
    {
        (1, "Introduction (1.1, 1.3)", "section-1"),
        (2, "Importance du problème/besoin (2)", "section-2"),
        (3, "Idées possibles (3.1 – 3.4)", "section-3"),
        (4, "Meilleure idée et titre (3.5, 1.2)", "section-35"),
        (5, "Conclusion et recommandations (4)", "section-4"),
    };
}
