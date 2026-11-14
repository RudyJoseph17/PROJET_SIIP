using System.Data;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using static IdentificationProjet.Infrastructure.PackageOracle;

namespace IdentificationProjet.Infrastructure;

/// <summary>Études et avis : lecture EF Core (V_IP_ETUDE*, V_IP_AVIS), écriture par PKG_IP_ETUDE.</summary>
public sealed class EtudeRepository : IEtudeRepository
{
    private const string Pkg = "PKG_IP_ETUDE.";
    private readonly IdentificationProjetDbContext _db;
    private readonly PackageOracle _pkg;

    public EtudeRepository(IdentificationProjetDbContext db)
    {
        _db = db;
        _pkg = new PackageOracle(db);
    }

    // ---------------------------------------------------------------- Lecture

    public Task<List<EtudeProjet>> ListerParFicheAsync(int idFiche, CancellationToken ct = default) =>
        _db.Etudes.Where(e => e.IdFiche == idFiche).OrderBy(e => e.OrdrePhase).ToListAsync(ct);

    public Task<List<EtudeProjet>> ListerParStatutsAsync(IEnumerable<string> statuts, string? ministere, CancellationToken ct = default)
    {
        var liste = statuts.ToList();
        var q = _db.Etudes.Where(e => liste.Contains(e.Statut));
        if (!string.IsNullOrWhiteSpace(ministere))
        {
            var m = ministere.Trim().ToUpper();
            q = q.Where(e => e.Ministere != null && e.Ministere.ToUpper() == m);
        }
        return q.OrderBy(e => e.DateSoumission).ToListAsync(ct);
    }

    public async Task<EtudeProjet?> ObtenirAsync(int idEtude, CancellationToken ct = default)
    {
        var e = await _db.Etudes.FirstOrDefaultAsync(x => x.IdEtude == idEtude, ct);
        if (e is null) return null;
        e.Documents = await _db.DocumentsEtude.Where(d => d.IdEtude == idEtude).OrderBy(d => d.DateAjout).ToListAsync(ct);
        e.Avis = await _db.Avis.Where(a => a.IdEtude == idEtude).OrderByDescending(a => a.Numero).ToListAsync(ct);
        return e;
    }

    public async Task<ContenuAnnexe?> LireDocumentAsync(int idEtude, int idDocument, CancellationToken ct = default)
    {
        var conn = await _pkg.ConnexionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.InitialLOBFetchSize = -1;
        cmd.CommandText = "SELECT d.nom_fichier, d.type_mime, d.contenu FROM tb_ip_etude_document d " +
                          "WHERE d.id_document = :id_document AND d.id_etude = :id_etude";
        cmd.Parameters.Add("id_document", OracleDbType.Int32, idDocument, ParameterDirection.Input);
        cmd.Parameters.Add("id_etude", OracleDbType.Int32, idEtude, ParameterDirection.Input);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var contenu = r.IsDBNull(2) ? Array.Empty<byte>() : (byte[])r.GetValue(2);
        return new ContenuAnnexe(r.GetString(0), r.IsDBNull(1) ? "application/octet-stream" : r.GetString(1), contenu);
    }

    public Task<List<AvisEtude>> ListerAvisAsync(IEnumerable<string>? statuts, CancellationToken ct = default)
    {
        var q = _db.Avis.AsQueryable();
        var liste = statuts?.ToList();
        if (liste is { Count: > 0 }) q = q.Where(a => liste.Contains(a.Statut));
        return q.OrderBy(a => a.DateCreation).ToListAsync(ct);
    }

    public Task<AvisEtude?> ObtenirAvisAsync(int idAvis, CancellationToken ct = default) =>
        _db.Avis.FirstOrDefaultAsync(a => a.IdAvis == idAvis, ct);

    // --------------------------------------------------------------- Écriture

    public Task MajAsync(int idEtude, string? description, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "MAJ_ETUDE");
            In(cmd, "p_id_etude", OracleDbType.Int32, idEtude);
            In(cmd, "p_description", OracleDbType.Clob, description);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task<int> AjouterDocumentAsync(int idEtude, NouvelleAnnexe d, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "AJOUTER_DOCUMENT");
            In(cmd, "p_id_etude", OracleDbType.Int32, idEtude);
            In(cmd, "p_nom_fichier", OracleDbType.Varchar2, d.NomFichier);
            In(cmd, "p_type_mime", OracleDbType.Varchar2, d.TypeMime);
            In(cmd, "p_contenu", OracleDbType.Blob, d.Contenu);
            In(cmd, "p_description", OracleDbType.Varchar2, d.Description);
            In(cmd, "p_ajoute_par", OracleDbType.Varchar2, d.AjoutePar);
            var id = cmd.Parameters.Add("p_id_document", OracleDbType.Decimal, ParameterDirection.Output);
            await cmd.ExecuteNonQueryAsync(ct);
            return ((OracleDecimal)id.Value).ToInt32();
        }, ct);

    public Task SupprimerDocumentAsync(int idEtude, int idDocument, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "SUPPRIMER_DOCUMENT");
            In(cmd, "p_id_etude", OracleDbType.Int32, idEtude);
            In(cmd, "p_id_document", OracleDbType.Int32, idDocument);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task ChangerStatutAsync(int idEtude, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "CHANGER_STATUT");
            In(cmd, "p_id_etude", OracleDbType.Int32, idEtude);
            In(cmd, "p_action", OracleDbType.Varchar2, action);
            In(cmd, "p_commentaire", OracleDbType.Varchar2, commentaire);
            In(cmd, "p_acteur_id", OracleDbType.Varchar2, acteurId);
            In(cmd, "p_acteur_nom", OracleDbType.Varchar2, acteurNom);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task EnregistrerAvisAsync(int idAvis, string? decision, string? commentaire, string? recommandations, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "MAJ_AVIS");
            In(cmd, "p_id_avis", OracleDbType.Int32, idAvis);
            In(cmd, "p_decision", OracleDbType.Varchar2, decision);
            In(cmd, "p_commentaire", OracleDbType.Clob, commentaire);
            In(cmd, "p_recommandations", OracleDbType.Clob, recommandations);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task ExecuterActionAvisAsync(int idAvis, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default) =>
        _pkg.ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, Pkg + "ACTION_AVIS");
            In(cmd, "p_id_avis", OracleDbType.Int32, idAvis);
            In(cmd, "p_action", OracleDbType.Varchar2, action);
            In(cmd, "p_commentaire", OracleDbType.Varchar2, commentaire);
            In(cmd, "p_acteur_id", OracleDbType.Varchar2, acteurId);
            In(cmd, "p_acteur_nom", OracleDbType.Varchar2, acteurNom);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);
}
