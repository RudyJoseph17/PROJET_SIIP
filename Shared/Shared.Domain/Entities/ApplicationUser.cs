using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Shared.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Ministere { get; set; }
        public string Entite { get; set; }
        //public string Privilege { get; set; }
        public string NomUtilisateur { get; set; }

        public bool MustChangePassword { get; set; } = true;

        // Navigation
        public ICollection<IdentityUserRole<string>> UserRoles { get; set; }
    }
}
