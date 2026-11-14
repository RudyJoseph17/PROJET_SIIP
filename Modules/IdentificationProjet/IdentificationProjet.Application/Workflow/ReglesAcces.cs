using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Application.Workflow;

/// <summary>Noms des rôles Identity utilisés par le module.</summary>
public static class RolesIdentification
{
    /// <summary>Unité d'Études et de Programmation (examen sectoriel).</summary>
    public const string Uep = "UEP";
    /// <summary>MPCE / DIP : consultation de toutes les fiches et grilles.</summary>
    public const string Dip = "DIP";
    /// <summary>Analyste de projet : remplit la grille d'analyse.</summary>
    public const string DipAnalyste = "DIP_ANALYSTE";
    /// <summary>Chef de Service : valide la grille ou la renvoie à l'analyste.</summary>
    public const string DipChefService = "DIP_CHEF_SERVICE";
    /// <summary>Directeur : « Vu et approuvé » — sa décision s'applique à la fiche.</summary>
    public const string DipDirecteur = "DIP_DIRECTEUR";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> Tous = new[] { Uep, Dip, DipAnalyste, DipChefService, DipDirecteur };
}

/// <summary>Utilisateur courant, tel que vu par les règles d'accès.</summary>
public sealed record ContexteUtilisateur(
    string UserId,
    string NomAffiche,
    string? Ministere,
    string? Courriel,
    bool EstUep,
    bool EstAnalyste,
    bool EstChefService,
    bool EstDirecteur,
    bool EstConsultantDip,
    bool EstAdmin)
{
    /// <summary>Membre du MPCE (DIP), quel que soit son rôle.</summary>
    public bool EstMpce => EstAnalyste || EstChefService || EstDirecteur || EstConsultantDip || EstAdmin;
}

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
        EstPromoteur(f, u) || u.EstMpce
        || (EstUepCompetente(f, u) && f.Statut != StatutFiche.Brouillon);

    /// <summary>
    /// Modifiable par le promoteur en brouillon et « à corriger » ; jamais pendant
    /// l'examen ni après validation (règle du type objet T_IP_FICHE.EST_MODIFIABLE).
    /// </summary>
    public static bool PeutModifier(FicheIdentification f, ContexteUtilisateur u) =>
        EstPromoteur(f, u) && f.EstModifiable;

    public static bool PeutSupprimer(FicheIdentification f, ContexteUtilisateur u) =>
        EstPromoteur(f, u) && f.Statut == StatutFiche.Brouillon;

    public static bool PeutSoumettre(FicheIdentification f, ContexteUtilisateur u) =>
        PeutModifier(f, u) && f.Progression >= 100;

    /// <summary>Actions d'examen de l'UEP. L'avis du MPCE passe par la grille d'analyse.</summary>
    public static IReadOnlyList<string> ActionsExamen(FicheIdentification f, ContexteUtilisateur u)
    {
        if (f.Statut == StatutFiche.SoumiseUep && EstUepCompetente(f, u))
            return new[] { ActionFiche.UepApprouver, ActionFiche.UepComplement, ActionFiche.UepRejeter };
        return Array.Empty<string>();
    }

    public static bool PeutExecuter(FicheIdentification f, ContexteUtilisateur u, string action) =>
        action == ActionFiche.Soumettre ? PeutSoumettre(f, u) : ActionsExamen(f, u).Contains(action);

    /// <summary>Le promoteur et l'UEP ne voient pas les échanges internes du MPCE.</summary>
    public static bool VoitEtapesInternesMpce(ContexteUtilisateur u) => u.EstMpce;

    private static bool MemeMinistere(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
        && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>Circuit de la grille : Analyste → Chef de Service → Directeur.</summary>
public static class ReglesGrille
{
    /// <summary>MPCE : toutes les grilles ; promoteur : les grilles approuvées de ses fiches.</summary>
    public static bool PeutConsulter(GrilleAnalyse g, FicheIdentification f, ContexteUtilisateur u) =>
        u.EstMpce || (ReglesAcces.EstPromoteur(f, u) && g.Statut == StatutGrille.Approuvee);

    public static bool PeutSaisir(GrilleAnalyse g, ContexteUtilisateur u) =>
        g.Statut == StatutGrille.EnAnalyse && (u.EstAnalyste || u.EstAdmin);

    /// <summary>
    /// Actions ouvertes à l'utilisateur. Séparation des tâches : on ne valide pas
    /// une grille qu'on a soi-même rédigée ou validée (sauf Admin).
    /// </summary>
    public static IReadOnlyList<string> Actions(GrilleAnalyse g, ContexteUtilisateur u)
    {
        switch (g.Statut)
        {
            case StatutGrille.EnAnalyse when u.EstAnalyste || u.EstAdmin:
                return new[] { ActionGrille.AnalysteTransmettre };

            case StatutGrille.AuChefService when u.EstAdmin || (u.EstChefService && g.AnalysteId != u.UserId):
                return new[] { ActionGrille.ChefValider, ActionGrille.ChefRenvoyer };

            case StatutGrille.AuDirecteur when u.EstAdmin
                || (u.EstDirecteur && g.AnalysteId != u.UserId && g.ChefId != u.UserId):
                return new[] { ActionGrille.DirecteurApprouver, ActionGrille.DirecteurRenvoyer };

            default:
                return Array.Empty<string>();
        }
    }

    /// <summary>Statuts de grille sur lesquels l'utilisateur doit agir (corbeille « À traiter »).</summary>
    public static IReadOnlyList<string> StatutsATraiter(ContexteUtilisateur u)
    {
        var s = new List<string>();
        if (u.EstAnalyste || u.EstAdmin) s.Add(StatutGrille.EnAnalyse);
        if (u.EstChefService || u.EstAdmin) s.Add(StatutGrille.AuChefService);
        if (u.EstDirecteur || u.EstAdmin) s.Add(StatutGrille.AuDirecteur);
        return s;
    }
}
