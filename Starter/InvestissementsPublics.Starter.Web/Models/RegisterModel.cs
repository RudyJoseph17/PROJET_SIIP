using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Domain.Dtos;
using Shared.Domain.Interfaces;

namespace InvestissementsPublics.Starter.Web.Models
{
    public class RegisterModel : PageModel
    {
        private readonly IUserRegistrationService _registrationService;

        public RegisterModel(IUserRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        [BindProperty]
        public RegisterUserRequest Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Input.Ministere) || string.IsNullOrWhiteSpace(Input.Entite))
            {
                ModelState.AddModelError(string.Empty, "Le ministère et l'entité sont obligatoires.");
                return Page();
            }

            Input.Ministere = Input.Ministere.Trim();
            Input.Entite = Input.Entite.Trim();

            var result = await _registrationService.RegisterAsync(Input);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);

                return Page();
            }

            ViewData["GeneratedPassword"] = result.GeneratedPassword;
            return Page();
        }
    }
}