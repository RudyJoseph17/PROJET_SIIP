using Shared.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Services
{
    public class PasswordGenerator : IPasswordGenerator
    {
        public string Generate()
        {
            return $"Inv{Guid.NewGuid().ToString().Substring(0, 8)}!";
        }
    }
}
