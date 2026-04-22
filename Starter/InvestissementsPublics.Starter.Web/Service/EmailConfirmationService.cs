using Microsoft.AspNetCore.Identity;
using Shared.Domain.Dtos;
using Shared.Domain.Entities;
using Shared.Domain.Interfaces;

namespace InvestissementsPublics.Starter.Web.Service
{
    public class EmailConfirmationService : IEmailConfirmationService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EmailConfirmationService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<EmailConfirmationResult> ConfirmEmailAsync(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                return new EmailConfirmationResult
                {
                    Succeeded = false,
                    Message = "ID utilisateur ou code manquant."
                };
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new EmailConfirmationResult
                {
                    Succeeded = false,
                    Message = $"Impossible de trouver l'utilisateur avec l'ID '{userId}'."
                };
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            return new EmailConfirmationResult
            {
                Succeeded = result.Succeeded,
                Message = result.Succeeded ? "Merci de confirmer votre email." : "Erreur lors de la confirmation de l'email."
            };
        }
    }
}
