namespace BanqueProjet.Application.Domain;

/// <summary>
/// Projet validé (études de projet approuvées par le MPCE), inscrit à la Banque de projets.
/// Lecture seule : vue V_BP_PROJET_VALIDE (alimentée par le module Identification projet).
/// </summary>
public class ProjetValide
{
    public int IdFiche { get; set; }
    public string CodeFiche { get; set; } = "";
    public string? TitreProjet { get; set; }
    public string? MinistereInstitution { get; set; }
    public string? Ministere { get; set; }
    public string? PromoteurNom { get; set; }
    public string? Departement { get; set; }
    public string? Commune { get; set; }
    public string? DescriptionProbleme { get; set; }
    public string? ObjectifGeneral { get; set; }
    public DateTime? DateValidationFiche { get; set; }
    public DateTime? DateValidationPrefaisabilite { get; set; }
    public DateTime? DateValidationFaisabilite { get; set; }
    public DateTime DateValidationProjet { get; set; }
}

public interface IProjetValideRepository
{
    Task<List<ProjetValide>> ListerAsync(CancellationToken ct = default);
    Task<ProjetValide?> ObtenirAsync(int idFiche, CancellationToken ct = default);
}
