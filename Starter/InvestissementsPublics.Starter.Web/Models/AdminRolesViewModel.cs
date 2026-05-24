using Shared.Domain.Entities;

namespace InvestissementsPublics.Starter.Web.Models
{
    public class AdminRolesViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public List<ApplicationRole> Roles { get; set; } = new();
        public List<Privilege> Privileges { get; set; } = new();

        public string NewUserName { get; set; } = string.Empty;
        public string NewUserEmail { get; set; } = string.Empty;
        public string NewUserPassword { get; set; } = string.Empty;
        public string NewUserNom { get; set; } = string.Empty;
        public string NewUserPrenom { get; set; } = string.Empty;
        public string NewUserMinistere { get; set; } = string.Empty;
        public string NewUserEntite { get; set; } = string.Empty;

        public string? EditUserId { get; set; }
        public string EditUserName { get; set; } = string.Empty;
        public string EditUserEmail { get; set; } = string.Empty;
        public string EditUserNom { get; set; } = string.Empty;
        public string EditUserPrenom { get; set; } = string.Empty;
        public string EditUserMinistere { get; set; } = string.Empty;
        public string EditUserEntite { get; set; } = string.Empty;

        public string NewRoleName { get; set; } = string.Empty;
        public string? EditRoleId { get; set; }
        public string EditRoleName { get; set; } = string.Empty;

        public string NewPrivilegeName { get; set; } = string.Empty;
        public string NewPrivilegeDescription { get; set; } = string.Empty;
        public int? EditPrivilegeId { get; set; }
        public string EditPrivilegeName { get; set; } = string.Empty;
        public string EditPrivilegeDescription { get; set; } = string.Empty;

        public string? RoleIdForPrivileges { get; set; }
        public List<int> SelectedPrivileges { get; set; } = new();
    }
}