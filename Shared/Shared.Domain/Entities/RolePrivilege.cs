using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class RolePrivilege
    {
        public string RoleId { get; set; }
        public ApplicationRole Role { get; set; }

        public int IdPrivilege { get; set; }
        public Privilege Privilege { get; set; }
    }
}
