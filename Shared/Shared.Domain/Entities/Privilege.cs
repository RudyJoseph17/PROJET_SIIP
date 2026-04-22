using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class Privilege
    {
        public int IdPrivilege { get; set; }

        public string NomPrivilege { get; set; } = string.Empty;

        public string DescriptionPrivilege { get; set; } = string.Empty;

        public ICollection<RolePrivilege> RolePrivileges { get; set; }
    }
}
