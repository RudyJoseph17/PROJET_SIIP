using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Interfaces;

namespace InvestissementsPublics.Starter.Web.Models
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly IEmailConfirmationService _emailConfirmationService;

        public ConfirmEmailModel(IEmailConfirmationService emailConfirmationService)
        {
            _emailConfirmationService = emailConfirmationService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            var result = await _emailConfirmationService.ConfirmEmailAsync(userId, code);
            StatusMessage = result.Message;

            if (!result.Succeeded && (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code)))
                return RedirectToPage("/Index");

            return Page();
        }
    }
}
