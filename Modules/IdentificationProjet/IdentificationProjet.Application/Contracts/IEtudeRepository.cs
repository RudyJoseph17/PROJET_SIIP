using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Application.Contracts;

/// <summary>Études et avis : lecture via V_IP_ETUDE / V_IP_AVIS, écriture via PKG_IP_ETUDE.</summary>
public interface IEtudeRepository
{
    Task<List<EtudeProjet>> ListerParFicheAsync(int idFiche, CancellationToken ct = default);
    Task<List<EtudeProjet>> ListerParStatutsAsync(IEnumerable<string> statuts, string? ministere, CancellationToken ct = default);
    /// <summary>Étude avec pièces jointes et avis ; null si introuvable.</summary>
    Task<EtudeProjet?> ObtenirAsync(int idEtude, CancellationToken ct = default);

    Task MajAsync(int idEtude, string? description, CancellationToken ct = default);
    Task<int> AjouterDocumentAsync(int idEtude, NouvelleAnnexe document, CancellationToken ct = default);
    Task SupprimerDocumentAsync(int idEtude, int idDocument, CancellationToken ct = default);
    Task<ContenuAnnexe?> LireDocumentAsync(int idEtude, int idDocument, CancellationToken ct = default);
    Task ChangerStatutAsync(int idEtude, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default);

    Task<List<AvisEtude>> ListerAvisAsync(IEnumerable<string>? statuts, CancellationToken ct = default);
    Task<AvisEtude?> ObtenirAvisAsync(int idAvis, CancellationToken ct = default);
    Task EnregistrerAvisAsync(int idAvis, string? decision, string? commentaire, string? recommandations, CancellationToken ct = default);
    Task ExecuterActionAvisAsync(int idAvis, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default);
}

/// <summary>Limites des pièces jointes d'une étude (identiques à PKG_IP_ETUDE).</summary>
public static class LimitesDocumentsEtude
{
    public const int NombreMax = 10;
    public const long TailleMaxOctets = 20 * 1024 * 1024;
}
