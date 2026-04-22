using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Dtos
{
    public class EmailConfirmationResult
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
