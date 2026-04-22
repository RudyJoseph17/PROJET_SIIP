namespace InvestissementsPublics.Starter.Web.Models
{
    public class RoleViewModel
    {
        public string IdRoles { get; set; }
        public string NomRole { get; set; }

        // Bonus pour la suite
        public List<string> Privileges { get; set; } = new();
    }
}
