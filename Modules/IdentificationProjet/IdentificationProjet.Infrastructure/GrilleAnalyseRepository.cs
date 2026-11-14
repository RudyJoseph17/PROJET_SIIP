using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using static IdentificationProjet.Infrastructure.PackageOracle;

namespace IdentificationProjet.Infrastructure;

/// <summary>Grilles d'analyse : lecture EF Core (vues V_IP_GRILLE*), écriture par le package.</summary>
public sealed class GrilleAnalyseRepository : IGrilleAnalyseRepository
{
    private readonly IdentificationProjetDbContext _db;
    private readonly PackageOracle _pkg;

    public GrilleAnalyseRepository(IdentificationProjetDbContext db)
    {
        _db = db;
        _pkg = new PackageOracle(db);
    }

    public Task<List<GrilleAnalyse>> ListerAsync(IEnumerable<string>? statuts, CancellationToken ct = default)
    {
        var q = _db.Grilles.AsQueryable();
        var liste = statuts?.ToList();
        if (liste is { Count: > 0 }) q = q.Where(g => liste.Contains(g.Statut));
        return q.OrderBy(g => g.DateReceptionDip).ToListAsync(ct);
    }

    public Task<List<GrilleAnalyse>> ListerParFicheAsync(int idFiche, CancellationToken ct = default) =>
        _db.Grilles.Where(g => g.IdFiche == idFiche).OrderByDescending(g => g.Numero).ToListAsync(ct);

    public async Task<GrilleAnalyse?> ObtenirAsync(int idGrille, CancellationToken ct = default)
    {
        var g = await _db.Grilles.FirstOrDefaultAsync(x => x.IdGrille == idGrille, ct);
        if (g is null) return null;
        g.Pistes = await _db.PistesGrille.Where(p => p.IdGrille == idGrille).OrderBy(p => p.Ordre).ToListAsync(ct);
        g.Reponses = await _db.ReponsesGrille.Where(r => r.IdGrille == idGrille).OrderBy(r => r.Numero).ToListAsync(ct);
        return g;
    }

    public Task EnregistrerAsync(SaisieGrille s, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            foreach (var r in s.Reponses)
            {
                await using var cmd = Proc(tx, "REPONDRE");
                In(cmd, "p_id_grille", OracleDbType.Int32, s.IdGrille);
                In(cmd, "p_numero", OracleDbType.Int32, r.Numero);
                In(cmd, "p_reponse", OracleDbType.Varchar2, r.Reponse);
                In(cmd, "p_commentaire", OracleDbType.Varchar2, r.Commentaire);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            await using (var cmd = Proc(tx, "MAJ_GRILLE"))
            {
                In(cmd, "p_id_grille", OracleDbType.Int32, s.IdGrille);
                In(cmd, "p_commentaires_description", OracleDbType.Clob, s.CommentairesDescription);
                In(cmd, "p_commentaires_generaux", OracleDbType.Clob, s.CommentairesGeneraux);
                In(cmd, "p_decision", OracleDbType.Varchar2, s.Decision);
                In(cmd, "p_recommandations", OracleDbType.Clob, s.Recommandations);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            return 0;
        }, ct);

    public Task ExecuterActionAsync(int idGrille, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "ACTION_GRILLE");
            In(cmd, "p_id_grille", OracleDbType.Int32, idGrille);
            In(cmd, "p_action", OracleDbType.Varchar2, action);
            In(cmd, "p_commentaire", OracleDbType.Varchar2, commentaire);
            In(cmd, "p_acteur_id", OracleDbType.Varchar2, acteurId);
            In(cmd, "p_acteur_nom", OracleDbType.Varchar2, acteurNom);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);
}
