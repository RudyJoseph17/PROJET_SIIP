using Shared.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Interfaces
{
    public interface IUserRegistrationService
    {
        Task<(bool Success, string GeneratedPassword, IEnumerable<string> Errors)>
            RegisterAsync(RegisterUserRequest request);
    }
}
