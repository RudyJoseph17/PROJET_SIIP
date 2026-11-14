using System.Data;
using IdentificationProjet.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.ManagedDataAccess.Client;

namespace IdentificationProjet.Infrastructure;

/// <summary>
/// Appels au package PKG_IP_IDENTIFICATION sur la connexion du DbContext :
/// transaction, liaison des paramètres et traduction des erreurs métier ORA-20xxx.
/// </summary>
internal sealed class PackageOracle
{
    private const string Package = "PKG_IP_IDENTIFICATION.";
    private readonly IdentificationProjetDbContext _db;

    public PackageOracle(IdentificationProjetDbContext db) => _db = db;

    public async Task<OracleConnection> ConnexionAsync(CancellationToken ct)
    {
        var conn = (OracleConnection)_db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await _db.Database.OpenConnectionAsync(ct);
        return conn;
    }

    /// <summary>Exécute une unité de travail en transaction ; traduit les ORA-20xxx.</summary>
    public async Task<T> ExecuterAsync<T>(Func<OracleTransaction, Task<T>> travail, CancellationToken ct)
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

    public static OracleCommand Proc(OracleTransaction tx, string procedure)
    {
        var cmd = tx.Connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = Package + procedure;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.BindByName = true;
        return cmd;
    }

    public static void In(OracleCommand cmd, string nom, OracleDbType type, object? valeur)
    {
        if (valeur is string s && string.IsNullOrWhiteSpace(s)) valeur = null;
        cmd.Parameters.Add(nom, type, valeur ?? DBNull.Value, ParameterDirection.Input);
    }

    /// <summary>"ORA-20003: message\nORA-06512: ..." -> "message".</summary>
    public static string MessageUtilisateur(string message)
    {
        var premiere = message.Split('\n')[0].Trim();
        var i = premiere.IndexOf(':');
        return premiere.StartsWith("ORA-", StringComparison.Ordinal) && i > 0 ? premiere[(i + 1)..].Trim() : premiere;
    }
}
