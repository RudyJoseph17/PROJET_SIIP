using System.Data;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace IdentificationProjet.Infrastructure;

/// <summary>
/// Lecture : EF Core sur les vues V_IP_*.
/// Écriture : appels ODP.NET au package PKG_IP_IDENTIFICATION (les tables objet
/// ne sont jamais modifiées directement par l'application).
/// </summary>
public sealed class FicheIdentificationRepository : IFicheIdentificationRepository
{
    private const string Package = "PKG_IP_IDENTIFICATION.";
    private readonly IdentificationProjetDbContext _db;

    public FicheIdentificationRepository(IdentificationProjetDbContext db) => _db = db;

    // ---------------------------------------------------------------- Lecture

    public Task<List<FicheIdentification>> ListerParPromoteurAsync(string promoteurId, CancellationToken ct = default) =>
        _db.Fiches.Where(f => f.PromoteurId == promoteurId)
                  .OrderByDescending(f => f.DateModification)
                  .ToListAsync(ct);

    public Task<List<FicheIdentification>> ListerParStatutsAsync(IEnumerable<string> statuts, string? ministere, CancellationToken ct = default)
    {
        var liste = statuts.ToList();
        var q = _db.Fiches.Where(f => liste.Contains(f.Statut));
        if (!string.IsNullOrWhiteSpace(ministere))
        {
            var m = ministere.Trim().ToUpper();
            q = q.Where(f => f.Ministere != null && f.Ministere.ToUpper() == m);
        }
        return q.OrderBy(f => f.DateSoumission).ToListAsync(ct);
    }

    public async Task<FicheIdentification?> ObtenirAsync(int idFiche, CancellationToken ct = default)
    {
        var fiche = await _db.Fiches.FirstOrDefaultAsync(f => f.IdFiche == idFiche, ct);
        if (fiche is null) return null;

        fiche.Idees = await _db.Idees.Where(i => i.IdFiche == idFiche)
                                     .OrderBy(i => i.Ordre).ThenBy(i => i.IdIdee).ToListAsync(ct);
        fiche.Annexes = await _db.Annexes.Where(a => a.IdFiche == idFiche)
                                         .OrderBy(a => a.DateAjout).ToListAsync(ct);
        fiche.Historique = await _db.Historique.Where(h => h.IdFiche == idFiche)
                                               .OrderBy(h => h.DateAction).ThenBy(h => h.IdHistorique).ToListAsync(ct);
        return fiche;
    }

    public async Task<ContenuAnnexe?> LireContenuAnnexeAsync(int idFiche, int idAnnexe, CancellationToken ct = default)
    {
        var conn = await ConnexionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.InitialLOBFetchSize = -1;
        cmd.CommandText = "SELECT a.nom_fichier, a.type_mime, a.contenu FROM tb_ip_annexe a " +
                          "WHERE a.id_annexe = :id_annexe AND a.id_fiche = :id_fiche";
        cmd.Parameters.Add("id_annexe", OracleDbType.Int32, idAnnexe, ParameterDirection.Input);
        cmd.Parameters.Add("id_fiche", OracleDbType.Int32, idFiche, ParameterDirection.Input);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        var contenu = r.IsDBNull(2) ? Array.Empty<byte>() : (byte[])r.GetValue(2);
        return new ContenuAnnexe(r.GetString(0), r.IsDBNull(1) ? "application/octet-stream" : r.GetString(1), contenu);
    }

    // --------------------------------------------------------------- Écriture

    public Task<int> CreerAsync(string promoteurId, string promoteurNom, string? ministere, string? courriel, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "CREER_FICHE");
            In(cmd, "p_promoteur_id", OracleDbType.Varchar2, promoteurId);
            In(cmd, "p_promoteur_nom", OracleDbType.Varchar2, promoteurNom);
            In(cmd, "p_ministere", OracleDbType.Varchar2, ministere);
            In(cmd, "p_courriel", OracleDbType.Varchar2, courriel);
            var id = cmd.Parameters.Add("p_id_fiche", OracleDbType.Decimal, ParameterDirection.Output);
            await cmd.ExecuteNonQueryAsync(ct);
            return ((OracleDecimal)id.Value).ToInt32();
        }, ct);

    public Task EnregistrerAsync(EnregistrementFiche s, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            // Sections 1.1, 1.3, 2.1, 2.2, 4.1
            await using (var cmd = Proc(tx, "MAJ_FICHE"))
            {
                In(cmd, "p_id_fiche", OracleDbType.Int32, s.IdFiche);
                In(cmd, "p_nom_organisation", OracleDbType.Varchar2, s.NomOrganisation);
                In(cmd, "p_nom_contact", OracleDbType.Varchar2, s.NomContact);
                In(cmd, "p_telephone", OracleDbType.Varchar2, s.Telephone);
                In(cmd, "p_courriel", OracleDbType.Varchar2, s.Courriel);
                In(cmd, "p_description_probleme", OracleDbType.Clob, s.DescriptionProbleme);
                In(cmd, "p_donnees_probleme", OracleDbType.Clob, s.DonneesProbleme);
                In(cmd, "p_consequences", OracleDbType.Clob, s.Consequences);
                In(cmd, "p_conclusions", OracleDbType.Clob, s.Conclusions);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // 3.1 – 3.4 : suppression des idées retirées du tableau
            var conservees = s.Idees.Where(i => i.IdIdee.HasValue).Select(i => i.IdIdee!.Value).ToHashSet();
            var existantes = await _db.Idees.Where(i => i.IdFiche == s.IdFiche).Select(i => i.IdIdee).ToListAsync(ct);
            foreach (var idSupprime in existantes.Where(id => !conservees.Contains(id)))
            {
                await using var cmd = Proc(tx, "SUPPRIMER_IDEE");
                In(cmd, "p_id_fiche", OracleDbType.Int32, s.IdFiche);
                In(cmd, "p_id_idee", OracleDbType.Int32, idSupprime);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Ajout / mise à jour, dans l'ordre du tableau
            var ids = new List<int>(s.Idees.Count);
            for (var i = 0; i < s.Idees.Count; i++)
            {
                var idee = s.Idees[i];
                await using var cmd = Proc(tx, "ENREGISTRER_IDEE");
                In(cmd, "p_id_fiche", OracleDbType.Int32, s.IdFiche);
                var pId = cmd.Parameters.Add("p_id_idee", OracleDbType.Decimal, ParameterDirection.InputOutput);
                pId.Value = idee.IdIdee.HasValue ? idee.IdIdee.Value : DBNull.Value;
                In(cmd, "p_ordre", OracleDbType.Int32, i + 1);
                In(cmd, "p_intitule", OracleDbType.Varchar2, idee.Intitule);
                In(cmd, "p_description", OracleDbType.Clob, idee.Description);
                In(cmd, "p_legitimite", OracleDbType.Clob, idee.Legitimite);
                In(cmd, "p_pertinence", OracleDbType.Clob, idee.Pertinence);
                In(cmd, "p_viabilite", OracleDbType.Clob, idee.Viabilite);
                await cmd.ExecuteNonQueryAsync(ct);
                ids.Add(((OracleDecimal)pId.Value).ToInt32());
            }

            // 3.5 + 1.2
            int? idMeilleure = s.IndexMeilleureIdee is int idx && idx >= 0 && idx < ids.Count ? ids[idx] : null;
            await using (var cmd = Proc(tx, "DEFINIR_MEILLEURE_IDEE"))
            {
                In(cmd, "p_id_fiche", OracleDbType.Int32, s.IdFiche);
                In(cmd, "p_id_idee", OracleDbType.Int32, idMeilleure);
                In(cmd, "p_justification", OracleDbType.Clob, s.JustificationChoix);
                In(cmd, "p_titre_projet", OracleDbType.Varchar2, s.TitreProjet);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            return 0;
        }, ct);

    public Task<int> AjouterAnnexeAsync(int idFiche, NouvelleAnnexe a, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "AJOUTER_ANNEXE");
            In(cmd, "p_id_fiche", OracleDbType.Int32, idFiche);
            In(cmd, "p_nom_fichier", OracleDbType.Varchar2, a.NomFichier);
            In(cmd, "p_type_mime", OracleDbType.Varchar2, a.TypeMime);
            In(cmd, "p_contenu", OracleDbType.Blob, a.Contenu);
            In(cmd, "p_description", OracleDbType.Varchar2, a.Description);
            In(cmd, "p_ajoute_par", OracleDbType.Varchar2, a.AjoutePar);
            var id = cmd.Parameters.Add("p_id_annexe", OracleDbType.Decimal, ParameterDirection.Output);
            await cmd.ExecuteNonQueryAsync(ct);
            return ((OracleDecimal)id.Value).ToInt32();
        }, ct);

    public Task SupprimerAnnexeAsync(int idFiche, int idAnnexe, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "SUPPRIMER_ANNEXE");
            In(cmd, "p_id_fiche", OracleDbType.Int32, idFiche);
            In(cmd, "p_id_annexe", OracleDbType.Int32, idAnnexe);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task ChangerStatutAsync(int idFiche, string action, string? commentaire, string acteurId, string acteurNom, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "CHANGER_STATUT");
            In(cmd, "p_id_fiche", OracleDbType.Int32, idFiche);
            In(cmd, "p_action", OracleDbType.Varchar2, action);
            In(cmd, "p_commentaire", OracleDbType.Varchar2, commentaire);
            In(cmd, "p_acteur_id", OracleDbType.Varchar2, acteurId);
            In(cmd, "p_acteur_nom", OracleDbType.Varchar2, acteurNom);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    public Task SupprimerAsync(int idFiche, CancellationToken ct = default) =>
        ExecuterAsync(async tx =>
        {
            await using var cmd = Proc(tx, "SUPPRIMER_FICHE");
            In(cmd, "p_id_fiche", OracleDbType.Int32, idFiche);
            await cmd.ExecuteNonQueryAsync(ct);
            return 0;
        }, ct);

    // ------------------------------------------------------------ Utilitaires

    private async Task<OracleConnection> ConnexionAsync(CancellationToken ct)
    {
        var conn = (OracleConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await _db.Database.OpenConnectionAsync(ct);
        return conn;
    }

    /// <summary>Exécute une unité de travail en transaction ; traduit les ORA-20xxx.</summary>
    private async Task<T> ExecuterAsync<T>(Func<OracleTransaction, Task<T>> travail, CancellationToken ct)
    {
        await ConnexionAsync(ct);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var resultat = await travail((OracleTransaction)tx.GetDbTransaction());
            await tx.CommitAsync(ct);
            return resultat;
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw new RegleMetierException(ex.Number, MessageUtilisateur(ex.Message), ex);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static OracleCommand Proc(OracleTransaction tx, string procedure)
    {
        var cmd = tx.Connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = Package + procedure;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.BindByName = true;
        return cmd;
    }

    private static void In(OracleCommand cmd, string nom, OracleDbType type, object? valeur)
    {
        if (valeur is string s && string.IsNullOrWhiteSpace(s)) valeur = null;
        cmd.Parameters.Add(nom, type, valeur ?? DBNull.Value, ParameterDirection.Input);
    }

    /// <summary>"ORA-20003: message\nORA-06512: ..." -> "message".</summary>
    internal static string MessageUtilisateur(string message)
    {
        var premiere = message.Split('\n')[0].Trim();
        var i = premiere.IndexOf(':');
        return premiere.StartsWith("ORA-", StringComparison.Ordinal) && i > 0 ? premiere[(i + 1)..].Trim() : premiere;
    }
}
