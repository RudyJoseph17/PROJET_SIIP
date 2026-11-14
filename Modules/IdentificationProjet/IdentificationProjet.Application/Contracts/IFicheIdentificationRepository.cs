using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Application.Contracts;

/// <summary>Accès aux fiches : lecture via les vues V_IP_*, écriture via PKG_IP_IDENTIFICATION.</summary>
public interface IFicheIdentificationRepository
{
    /// <summary>Fiches créées par le promoteur.</summary>
    Task<List<FicheIdentification>> ListerParPromoteurAsync(string promoteurId, CancellationToken ct = default);

    /// <summary>Fiches dans un des statuts donnés (ministère facultatif, pour l'UEP).</summary>
    Task<List<FicheIdentification>> ListerParStatutsAsync(IEnumerable<string> statuts, string? ministere, CancellationToken ct = default);

    /// <summary>Fiche avec ses idées, annexes et historique ; null si introuvable.</summary>
    Task<FicheIdentification?> ObtenirAsync(int idFiche, CancellationToken ct = default);

    Task<int> CreerAsync(string promoteurId, string promoteurNom, string? ministere, string? courriel, CancellationToken ct = default);

    /// <summary>Enregistre toutes les sections saisies en une transaction.</summary>
    Task EnregistrerAsync(EnregistrementFiche saisie, CancellationToken ct = default);

    Task<int> AjouterAnnexeAsync(int idFiche, NouvelleAnnexe annexe, CancellationToken ct = default);
    Task SupprimerAnnexeAsync(int idFiche, int idAnnexe, CancellationToken ct = default);
    Task<ContenuAnnexe?> LireContenuAnnexeAsync(int idFiche, int idAnnexe, CancellationToken ct = default);

    Task ChangerStatutAsync(int idFiche, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default);

    Task SupprimerAsync(int idFiche, CancellationToken ct = default);
}

/// <summary>Données saisies dans le formulaire de la fiche.</summary>
public sealed class EnregistrementFiche
{
    public int IdFiche { get; init; }

    // 1.1
    public string? NomOrganisation { get; init; }
    public string? NomContact { get; init; }
    public string? Telephone { get; init; }
    public string? Courriel { get; init; }
    // 1.3, 2.1, 2.2, 4.1
    public string? DescriptionProbleme { get; init; }
    public string? DonneesProbleme { get; init; }
    public string? Consequences { get; init; }
    public string? Conclusions { get; init; }

    // 3.1 – 3.4 (ordre = position dans la liste)
    public IReadOnlyList<SaisieIdee> Idees { get; init; } = Array.Empty<SaisieIdee>();

    // 3.5 : index (dans Idees) de la meilleure idée, null si aucune
    public int? IndexMeilleureIdee { get; init; }
    public string? JustificationChoix { get; init; }

    // 1.2 : ignoré tant qu'aucune meilleure idée n'est déterminée
    public string? TitreProjet { get; init; }
}

public sealed class SaisieIdee
{
    /// <summary>null pour une nouvelle idée.</summary>
    public int? IdIdee { get; init; }
    public string? Intitule { get; init; }
    public string? Description { get; init; }
    public string? Legitimite { get; init; }
    public string? Pertinence { get; init; }
    public string? Viabilite { get; init; }
}

public sealed record NouvelleAnnexe(string NomFichier, string TypeMime, byte[] Contenu, string? Description, string AjoutePar);

public sealed record ContenuAnnexe(string NomFichier, string TypeMime, byte[] Contenu);

/// <summary>Règle métier refusée par la base (ORA-20xxx) ; le message est destiné à l'utilisateur.</summary>
public sealed class RegleMetierException : Exception
{
    public int Code { get; }
    public RegleMetierException(int code, string message, Exception? inner = null) : base(message, inner) => Code = code;
}
