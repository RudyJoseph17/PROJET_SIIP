using System.Text;
using IdentificationProjet.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IdentificationProjet.Infrastructure;

/// <summary>
/// Contexte EF Core en LECTURE SEULE : chaque entité est mappée sur une vue
/// relationnelle (V_IP_*) qui expose une table objet Oracle (TB_IP_*).
/// Le schéma est créé par les scripts du dossier Database (pas de migration EF).
/// </summary>
public class IdentificationProjetDbContext : DbContext
{
    public IdentificationProjetDbContext(DbContextOptions<IdentificationProjetDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<FicheIdentification> Fiches => Set<FicheIdentification>();
    public DbSet<IdeePossible> Idees => Set<IdeePossible>();
    public DbSet<AnnexeFiche> Annexes => Set<AnnexeFiche>();
    public DbSet<HistoriqueFiche> Historique => Set<HistoriqueFiche>();
    public DbSet<Lieu> Lieux => Set<Lieu>();
    public DbSet<GrilleAnalyse> Grilles => Set<GrilleAnalyse>();
    public DbSet<PisteGrille> PistesGrille => Set<PisteGrille>();
    public DbSet<ReponseGrille> ReponsesGrille => Set<ReponseGrille>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<FicheIdentification>(e =>
        {
            e.ToView("V_IP_FICHE").HasKey(x => x.IdFiche);
            e.HasMany(x => x.Idees).WithOne().HasForeignKey(i => i.IdFiche);
            e.HasMany(x => x.Annexes).WithOne().HasForeignKey(a => a.IdFiche);
            e.HasMany(x => x.Historique).WithOne().HasForeignKey(h => h.IdFiche);
        });
        b.Entity<IdeePossible>().ToView("V_IP_IDEE_POSSIBLE").HasKey(x => x.IdIdee);
        b.Entity<AnnexeFiche>().ToView("V_IP_ANNEXE").HasKey(x => x.IdAnnexe);
        b.Entity<HistoriqueFiche>().ToView("V_IP_HISTORIQUE").HasKey(x => x.IdHistorique);
        b.Entity<Lieu>().ToView("V_IP_LIEU").HasKey(x => x.IdLieu);

        // Grille : les tables imbriquées PISTES et REPONSES sont dépliées par des vues
        b.Entity<GrilleAnalyse>(e =>
        {
            e.ToView("V_IP_GRILLE").HasKey(x => x.IdGrille);
            e.HasMany(x => x.Pistes).WithOne().HasForeignKey(p => p.IdGrille);
            e.HasMany(x => x.Reponses).WithOne().HasForeignKey(r => r.IdGrille);
        });
        b.Entity<PisteGrille>().ToView("V_IP_GRILLE_PISTE").HasKey(x => new { x.IdGrille, x.Ordre });
        b.Entity<ReponseGrille>().ToView("V_IP_GRILLE_REPONSE").HasKey(x => new { x.IdGrille, x.Numero });

        // Convention Oracle : PropriétéPascal -> COLONNE_MAJUSCULE ; booléens en NUMBER(1)
        var boolConverter = new BoolToZeroOneConverter<short>();
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var p in entity.GetProperties())
            {
                p.SetColumnName(VersColonneOracle(p.Name));
                if (p.ClrType == typeof(bool))
                {
                    p.SetValueConverter(boolConverter);
                    p.SetColumnType("NUMBER(1)");
                }
            }
        }
    }

    internal static string VersColonneOracle(string nom)
    {
        var sb = new StringBuilder(nom.Length + 8);
        for (var i = 0; i < nom.Length; i++)
        {
            var c = nom[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(nom[i - 1])) sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }
}
