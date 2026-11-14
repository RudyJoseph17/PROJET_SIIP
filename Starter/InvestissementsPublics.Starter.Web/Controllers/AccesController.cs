using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    /// <summary>Page affichée quand un utilisateur connecté n'a pas le rôle requis (AccessDeniedPath).</summary>
    [Authorize]
    public class AccesController : Controller
    {
        public IActionResult Refuse(string? returnUrl = null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}
