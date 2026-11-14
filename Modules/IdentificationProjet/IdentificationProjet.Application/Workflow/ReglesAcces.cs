using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Application.Workflow;

/// <summary>Noms des rôles Identity utilisés par le module.</summary>
public static class RolesIdentification
{
    /// <summary>Unité d'Études et de Programmation (examen sectoriel).</summary>
    public const string Uep = "UEP";
    /// <summary>Direction de l'Investissement Public (MPCE).</summary>
    public const string Dip = "DIP";
    public const string Admin = "Admin";
}

/// <summary>Utilisateur courant, tel que vu par les règles d'accès.</summary>
public sealed record ContexteUtilisateur(
    string UserId,
    string NomAffiche,
    string? Ministere,
    string? Courriel,
    bool EstUep,
    bool EstDip,
    bool EstAdmin);

/// <summary>
/// Qui peut voir / modifier / examiner une fiche. Le statut est revérifié
/// par PKG_IP_IDENTIFICATION ; ces règles portent sur les personnes.
/// </summary>
public static class ReglesAcces
{
    public static bool EstPromoteur(FicheIdentification f, ContexteUtilisateur u) =>
        f.PromoteurId == u.UserId;

    /// <summary>L'UEP examine les fiches de son ministère (secteur).</summary>
    public static bool EstUepCompetente(FicheIdentification f, ContexteUtilisateur u) =>
        u.EstAdmin || (u.EstUep && MemeMinistere(f.Ministere, u.Ministere));

    public static bool PeutConsulter(FicheIdentification f, ContexteUtilisateur u) =>
        EstPromoteur(f, u) || u.EstAdmin || u.EstDip
        || (EstUepCompetente(f, u) && f.Statut != StatutFiche.Brouillon);

    public static bool PeutModifier(FicheIdentification f, ContexteUtilisateur u) =>
        EstPromoteur(f, u) && f.EstModifiable;

    public static bool PeutSupprimer(FicheIdentification f, ContexteUtilisateur u) =>
        EstPromoteur(f, u) && f.Statut == StatutFiche.Brouillon;

    public static bool PeutSoumettre(FicheIdentification f, ContexteUtilisateur u) =>
        PeutModifier(f, u) && f.Progression >= 100;

    /// <summary>Actions d'examen (UEP / DIP) ouvertes à l'utilisateur sur cette fiche.</summary>
    public static IReadOnlyList<string> ActionsExamen(FicheIdentification f, ContexteUtilisateur u)
    {
        if (f.Statut == StatutFiche.SoumiseUep && EstUepCompetente(f, u))
            return new[] { ActionFiche.UepApprouver, ActionFiche.UepComplement, ActionFiche.UepRejeter };

        if (f.Statut == StatutFiche.TransmiseDip && (u.EstDip || u.EstAdmin))
            return new[] { ActionFiche.DipApprouver, ActionFiche.DipComplement, ActionFiche.DipRejeter };

        return Array.Empty<string>();
    }

    public static bool PeutExecuter(FicheIdentification f, ContexteUtilisateur u, string action) =>
        action == ActionFiche.Soumettre ? PeutSoumettre(f, u) : ActionsExamen(f, u).Contains(action);

    private static bool MemeMinistere(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
