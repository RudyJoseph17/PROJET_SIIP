namespace InvestissementsPublics.Starter.Web.Securite
{
    /// <summary>Rôles utilisés par l'administration de la sécurité.</summary>
    public static class RolesSecurite
    {
        public const string Admin = "Admin";
        public const string GestionnaireSysteme = "Gestionnaire systeme";

        /// <summary>Valeur de [Authorize(Roles = ...)] pour les pages Sécurité.</summary>
        public const string Administrateurs = Admin + "," + GestionnaireSysteme;

        /// <summary>
        /// Rôles dont le nom est utilisé dans le code (contrôles d'accès, seeder, module
        /// Identification projet) : ils ne peuvent être ni renommés ni supprimés, mais
        /// leurs privilèges restent modifiables.
        /// </summary>
        public static readonly IReadOnlySet<string> Systeme = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Admin, GestionnaireSysteme,
            "UEP", "DIP", "DIP_ANALYSTE", "DIP_CHEF_SERVICE", "DIP_DIRECTEUR",
        };

        public static bool EstSysteme(string? nomRole) => nomRole is not null && Systeme.Contains(nomRole);

        /// <summary>
        /// Suppression d'un rôle : tout administrateur pour un rôle ordinaire ; seul le rôle Admin
        /// peut supprimer un rôle système (rôles encore en cours de définition). Le rôle Admin
        /// lui-même n'est jamais supprimable (il donne accès à cette page).
        /// </summary>
        public static bool PeutSupprimer(string? nomRole, bool utilisateurEstAdmin) =>
            !string.Equals(nomRole, Admin, StringComparison.OrdinalIgnoreCase)
            && (!EstSysteme(nomRole) || utilisateurEstAdmin);
    }
}
