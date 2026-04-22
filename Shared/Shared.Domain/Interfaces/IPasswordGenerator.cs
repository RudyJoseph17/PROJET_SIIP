using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Domain.Dtos;

namespace Shared.Domain.Interfaces
{
    public interface IPasswordGenerator
    {
        string Generate();
    }
}
