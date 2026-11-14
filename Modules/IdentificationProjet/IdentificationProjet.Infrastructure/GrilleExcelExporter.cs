using System.Globalization;
using ClosedXML.Excel;
using IdentificationProjet.Application.Contracts;
using IdentificationProjet.Application.Domain;

namespace IdentificationProjet.Infrastructure;

/// <summary>
/// Remplit le modèle officiel « GRILLE D'ANALYSE - IDEE PROJET » (feuille GRILLE_ANALYSE)
/// avec une grille enregistrée. Les cellules ciblées sont celles du modèle fourni.
/// </summary>
public sealed class GrilleExcelExporter : IGrilleExcelExporter
{
    private const string Ressource = "GrilleAnalyseModele.xlsx";
    private const string Feuille = "GRILLE_ANALYSE";
    private const int PremiereLigneQuestions = 40;   // question 1 en ligne 40 … question 11 en ligne 50
    private const int NbQuestionsModele = 11;
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public byte[] Exporter(GrilleAnalyse g)
    {
        using var modele = typeof(GrilleExcelExporter).Assembly.GetManifestResourceStream(Ressource)
            ?? throw new InvalidOperationException($"Modèle Excel introuvable ({Ressource}).");
        using var wb = new XLWorkbook(modele);
        var ws = wb.Worksheet(Feuille);

        // En-tête
        Ajouter(ws, "A15", "MINISTÈRE / INSTITUTION : ", g.Ministere);
        Ajouter(ws, "A16", "TITRE DE L'IDEE DE PROJET : ", g.TitreIdee);
        Ajouter(ws, "A17", "SOUMIS PAR L’UEP LE : ", Date(g.DateSoumissionUep));
        Ajouter(ws, "A18", "RECU PAR LA DIP LE : ", Date(g.DateReceptionDip));

        // 1- Localisation
        Valeur(ws, "B22", g.Departement);
        Valeur(ws, "D22", g.Arrondissement);
        Valeur(ws, "B23", g.Commune);
        Valeur(ws, "D23", g.SectionCommunale);
        Ajouter(ws, "A24", "Localité : ", g.Localite);
        Valeur(ws, "D24", g.Milieu);

        // 2- à 7- : copie de la fiche
        Ajouter(ws, "A25", "2- Problème à résoudre : ", g.Probleme, texteLong: true);
        Ajouter(ws, "A26", "3- Objectif général : ", g.ObjectifGeneral, texteLong: true);
        Ajouter(ws, "A27", "3- Données quantitatives : ", g.DonneesQuantitatives, texteLong: true);
        Ajouter(ws, "A28", "4- Données qualitatives : ", g.DonneesQualitatives, texteLong: true);
        var pistes = string.Join("\n", g.Pistes.Select(p =>
            $"{p.Ordre}. {p.Intitule}{(string.IsNullOrWhiteSpace(p.Description) ? "" : " — " + p.Description)}"));
        Ajouter(ws, "A29", "5- Pistes de solutions :\n", pistes, texteLong: true);
        Ajouter(ws, "A30", "6- Meilleure idée : ", g.MeilleureIdee);
        Valeur(ws, "C31", g.PertinenceChoisie, texteLong: true);
        Valeur(ws, "A32",
            $"Légitimité : {g.LegitimiteChoisie}\nViabilité : {g.ViabiliteChoisie}", texteLong: true);
        Ajouter(ws, "C36", "COMMENTAIRES : ", g.CommentairesDescription, texteLong: true);

        // Généralités : réponse (Oui/Non) en D, commentaire (si Non) dans une nouvelle colonne E
        var entete = ws.Cell("E39");
        entete.Value = "COMMENTAIRES";
        entete.Style = ws.Cell("D39").Style;
        ws.Column("E").Width = Math.Max(ws.Column("E").Width, 45);
        foreach (var r in g.Reponses.Where(r => r.Numero is >= 1 and <= NbQuestionsModele))
        {
            var ligne = PremiereLigneQuestions + r.Numero - 1;
            Valeur(ws, $"D{ligne}", r.Reponse switch { "OUI" => "Oui", "NON" => "Non", _ => "" });
            var c = ws.Cell($"E{ligne}");
            c.Style = ws.Cell($"D{ligne}").Style;
            c.Value = r.Reponse == "NON" ? r.Commentaire ?? "" : "";
            c.Style.Alignment.WrapText = true;
        }

        Valeur(ws, "A54", g.CommentairesGeneraux, texteLong: true);

        // Avis : libellé exact de la liste DECISIONS du modèle
        Valeur(ws, "A64", LibelleDecisionModele(wb, g.Decision));
        Valeur(ws, "A68", g.Recommandations, texteLong: true);

        // Signatures
        Ajouter(ws, "A77", "Analyste de projet : ", g.AnalysteNom);
        Ajouter(ws, "C77", "le : ", Date(g.DateAnalyste));
        Ajouter(ws, "A79", "Chef de Service : ", g.ChefNom);
        Ajouter(ws, "C79", "le : ", Date(g.DateChef));
        Ajouter(ws, "A81", "Vu et Approuvé par : ", g.DirecteurNom);
        Ajouter(ws, "C81", "le : ", Date(g.DateDirecteur));

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string Date(DateTime? d) => d?.ToString("dd/MM/yyyy", Fr) ?? "";

    private static void Ajouter(IXLWorksheet ws, string adresse, string libelle, string? valeur, bool texteLong = false) =>
        Valeur(ws, adresse, libelle + (valeur ?? ""), texteLong);

    private static void Valeur(IXLWorksheet ws, string adresse, string? valeur, bool texteLong = false)
    {
        var c = ws.Cell(adresse);
        c.Value = valeur ?? "";
        if (!texteLong) return;

        c.Style.Alignment.WrapText = true;
        c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        // Les cellules fusionnées ne s'ajustent pas seules : hauteur estimée (≈ 95 caractères par ligne)
        var plage = c.MergedRange();
        var nbLignesFeuille = plage?.RowCount() ?? 1;
        var lignesTexte = (valeur ?? "").Split('\n').Sum(l => Math.Max(1, (int)Math.Ceiling(l.Length / 95.0)));
        var hauteur = Math.Min(409, 15 * lignesTexte / (double)nbLignesFeuille);
        var ligne = ws.Row(c.Address.RowNumber);
        if (hauteur > ligne.Height) ligne.Height = hauteur;
    }

    /// <summary>Cherche le libellé exact (espaces compris) dans la plage nommée DECISIONS.</summary>
    private static string LibelleDecisionModele(XLWorkbook wb, string? decision)
    {
        var attendu = DecisionGrille.Libelle(decision);
        if (attendu.Length == 0) return "";
        try
        {
            var plage = wb.NamedRange("DECISIONS")?.Ranges.FirstOrDefault();
            var trouve = plage?.Cells().Select(c => c.GetString())
                .FirstOrDefault(v => v.Trim().Equals(attendu, StringComparison.OrdinalIgnoreCase));
            return trouve ?? attendu;
        }
        catch
        {
            return attendu;
        }
    }
}
