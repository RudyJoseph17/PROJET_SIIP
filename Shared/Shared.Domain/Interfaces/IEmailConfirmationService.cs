using Shared.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Interfaces
{
    public interface IEmailConfirmationService
    {
        Task<EmailConfirmationResult> ConfirmEmailAsync(string userId, string code);
    }
}
