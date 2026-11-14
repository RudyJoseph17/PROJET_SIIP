using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentificationProjet.Web.Areas.IdentificationProjet.Controllers;

/// <summary>Point d'entrée du module (lien du menu latéral) : redirige vers la liste des fiches.</summary>
[Area("IdentificationProjet")]
[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Fiches");
}
