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
