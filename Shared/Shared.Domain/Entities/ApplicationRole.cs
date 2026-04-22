using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public ICollection<RolePrivilege> RolePrivileges { get; set; }
    }
}
