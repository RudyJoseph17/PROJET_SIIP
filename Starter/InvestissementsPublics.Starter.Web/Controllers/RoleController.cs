using InvestissementsPublics.Starter.Web.Securite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    /// <summary>
    /// Ancienne page « Administration utilisateurs, rôles et privilèges » (/Role/Admin),
    /// remplacée par les pages Utilisateurs, Rôles et Privilèges. Conservée pour les liens existants.
    /// </summary>
    [Authorize(Roles = RolesSecurite.Administrateurs)]
    public class RoleController : Controller
    {
        public IActionResult Admin() => RedirectToAction("Index", "Utilisateurs");
    }
}
