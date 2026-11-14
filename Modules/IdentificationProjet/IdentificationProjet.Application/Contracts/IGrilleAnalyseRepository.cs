using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Application.Contracts;

/// <summary>Grilles d'analyse : lecture via V_IP_GRILLE*, écriture via PKG_IP_IDENTIFICATION.</summary>
public interface IGrilleAnalyseRepository
{
    /// <summary>Grilles dans les statuts donnés (toutes si vide), sans le détail.</summary>
    Task<List<GrilleAnalyse>> ListerAsync(IEnumerable<string>? statuts, CancellationToken ct = default);

    /// <summary>Grilles d'une fiche (la plus récente en premier), sans le détail.</summary>
    Task<List<GrilleAnalyse>> ListerParFicheAsync(int idFiche, CancellationToken ct = default);

    /// <summary>Grille avec ses pistes et ses réponses ; null si introuvable.</summary>
    Task<GrilleAnalyse?> ObtenirAsync(int idGrille, CancellationToken ct = default);

    /// <summary>Enregistre la saisie de l'analyste (réponses + commentaires + résultat) en une transaction.</summary>
    Task EnregistrerAsync(SaisieGrille saisie, CancellationToken ct = default);

    Task ExecuterActionAsync(int idGrille, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default);
}

public sealed class SaisieGrille
{
    public int IdGrille { get; init; }
    public string? CommentairesDescription { get; init; }
    public string? CommentairesGeneraux { get; init; }
    public string? Decision { get; init; }
    public string? Recommandations { get; init; }
    public IReadOnlyList<SaisieReponse> Reponses { get; init; } = Array.Empty<SaisieReponse>();
}

public sealed record SaisieReponse(int Numero, string? Reponse, string? Commentaire);

/// <summary>Export de la grille au format Excel du MPCE (modèle GRILLE D'ANALYSE - IDEE PROJET).</summary>
public interface IGrilleExcelExporter
{
    byte[] Exporter(GrilleAnalyse grille);
}
