using System.ComponentModel.DataAnnotations;
using Shared.Domain.Entities;

namespace InvestissementsPublics.Starter.Web.Models.Securite
{
    // =========================================================== Utilisateurs

    public class UtilisateurLigneViewModel
    {
        public string Id { get; set; } = "";
        public string? NomUtilisateur { get; set; }
        public string? NomComplet { get; set; }
        public string? Ministere { get; set; }
        public string? Entite { get; set; }
    }

    /// <summary>Formulaire « Nouvel utilisateur » / « Modification utilisateur ».</summary>
    public class UtilisateurFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
        [StringLength(256)]
        [RegularExpression(@"^[a-zA-Z0-9._@+-]+$", ErrorMessage = "Lettres sans accents, chiffres et . _ @ + - uniquement.")]
        [Display(Name = "Nom d'utilisateur")]
        public string NomUtilisateur { get; set; } = "";

        [Required(ErrorMessage = "L'adresse électronique est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse électronique invalide.")]
        [Display(Name = "Adresse électronique")]
        public string Email { get; set; } = "";

        // Colonnes NOT NULL dans AspNetUsers (et pour Oracle, chaîne vide = NULL) : obligatoires
        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100), Display(Name = "Nom")]
        public string Nom { get; set; } = "";

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100), Display(Name = "Prénom")]
        public string Prenom { get; set; } = "";

        [Required(ErrorMessage = "Le ministère est obligatoire.")]
        [StringLength(200), Display(Name = "Ministère")]
        public string Ministere { get; set; } = "";

        [Required(ErrorMessage = "L'entité est obligatoire.")]
        [StringLength(200), Display(Name = "Entité")]
        public string Entite { get; set; } = "";

        [Display(Name = "Rôles")]
        public List<string> RoleIds { get; set; } = new();

        /// <summary>Liste des rôles proposés (affichage).</summary>
        public List<ApplicationRole> RolesDisponibles { get; set; } = new();
    }

    public class UtilisateurDetailsViewModel
    {
        public required ApplicationUser Utilisateur { get; init; }
        public List<string> Roles { get; init; } = new();
        public bool EstUtilisateurCourant { get; init; }
    }

    // ================================================================= Rôles

    public class RoleLigneViewModel
    {
        public string Id { get; set; } = "";
        public string Nom { get; set; } = "";
        public int NbUtilisateurs { get; set; }
        public int NbPrivileges { get; set; }
        public bool EstSysteme { get; set; }
    }

    public class RoleFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Le nom du rôle est obligatoire.")]
        [StringLength(256), Display(Name = "Nom du rôle")]
        public string Nom { get; set; } = "";

        [Display(Name = "Privilèges")]
        public List<int> PrivilegeIds { get; set; } = new();

        public List<Privilege> PrivilegesDisponibles { get; set; } = new();
        public bool EstSysteme { get; set; }
    }

    public class RoleDetailsViewModel
    {
        public required ApplicationRole Role { get; init; }
        public List<Privilege> Privileges { get; init; } = new();
        public List<ApplicationUser> Utilisateurs { get; init; } = new();
        public bool EstSysteme { get; init; }
    }

    // ============================================================ Privilèges

    public class PrivilegeLigneViewModel
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string? Description { get; set; }
        public int NbRoles { get; set; }
    }

    public class PrivilegeFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Le nom du privilège est obligatoire.")]
        [StringLength(200), Display(Name = "Nom du privilège")]
        public string Nom { get; set; } = "";

        [StringLength(500), Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class PrivilegeDetailsViewModel
    {
        public required Privilege Privilege { get; init; }
        public List<ApplicationRole> Roles { get; init; } = new();
    }
}
