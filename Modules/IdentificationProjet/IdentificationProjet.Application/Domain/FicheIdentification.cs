namespace IdentificationProjet.Application.Domain;

/// <summary>
/// Fiche d'Identification de l'Idée de Projet (Manuel de procédures, Annexe 3).
/// Lecture seule : projection de la vue V_IP_FICHE sur la table objet TB_IP_FICHE.
/// Les écritures passent par le package PKG_IP_IDENTIFICATION (voir le dépôt).
/// </summary>
public class FicheIdentification
{
    public int IdFiche { get; set; }
    public string CodeFiche { get; set; } = "";

    // 1.1 Contact principal
    public string? NomOrganisation { get; set; }
    public string? NomContact { get; set; }
    public string? Telephone { get; set; }
    public string? Courriel { get; set; }

    // 1.2 Titre proposé (renseigné après le choix de la meilleure idée)
    public string? TitreProjet { get; set; }

    // 1.3 Description du problème / besoin
    public string? DescriptionProbleme { get; set; }

    // 2. Importance du problème/besoin
    public string? DonneesProbleme { get; set; }   // 2.1
    public string? Consequences { get; set; }      // 2.2

    // 3.5 Justification du choix de la meilleure idée
    public string? JustificationChoix { get; set; }

    // 4.1 Conclusions et recommandations
    public string? Conclusions { get; set; }

    // Suivi
    public string Statut { get; set; } = StatutFiche.Brouillon;
    public string PromoteurId { get; set; } = "";
    public string? PromoteurNom { get; set; }
    public string? Ministere { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
    public DateTime? DateSoumission { get; set; }

    // Calculés par les méthodes du type objet T_IP_FICHE
    public bool Etape1 { get; set; }
    public bool Etape2 { get; set; }
    public bool Etape3 { get; set; }
    public bool Etape4 { get; set; }
    public bool Etape5 { get; set; }
    public int Progression { get; set; }
    public bool EstModifiable { get; set; }
    public int NbIdees { get; set; }
    public int NbAnnexes { get; set; }
    public string? MeilleureIdee { get; set; }

    public List<IdeePossible> Idees { get; set; } = new();
    public List<AnnexeFiche> Annexes { get; set; } = new();
    public List<HistoriqueFiche> Historique { get; set; } = new();

    public bool EtapeComplete(int etape) => etape switch
    {
        1 => Etape1,
        2 => Etape2,
        3 => Etape3,
        4 => Etape4,
        5 => Etape5,
        _ => false
    };
}

/// <summary>Sections 3.1 à 3.4 : une idée possible et son analyse.</summary>
public class IdeePossible
{
    public int IdIdee { get; set; }
    public int IdFiche { get; set; }
    public int Ordre { get; set; }
    public string? Intitule { get; set; }
    public string? Description { get; set; }
    public string? Legitimite { get; set; }
    public string? Pertinence { get; set; }
    public string? Viabilite { get; set; }
    public bool EstMeilleure { get; set; }
    public bool EstComplete { get; set; }
}

/// <summary>Section 5 : document annexe (le contenu binaire n'est chargé qu'au téléchargement).</summary>
public class AnnexeFiche
{
    public int IdAnnexe { get; set; }
    public int IdFiche { get; set; }
    public string NomFichier { get; set; } = "";
    public string? TypeMime { get; set; }
    public long Taille { get; set; }
    public string? Description { get; set; }
    public DateTime DateAjout { get; set; }
    public string? AjoutePar { get; set; }
}

public class HistoriqueFiche
{
    public int IdHistorique { get; set; }
    public int IdFiche { get; set; }
    public string Action { get; set; } = "";
    public string? AncienStatut { get; set; }
    public string? NouveauStatut { get; set; }
    public string? Commentaire { get; set; }
    public string? ActeurId { get; set; }
    public string? ActeurNom { get; set; }
    public DateTime DateAction { get; set; }
}
