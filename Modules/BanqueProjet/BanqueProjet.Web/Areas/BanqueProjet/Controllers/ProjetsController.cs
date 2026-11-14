using BanqueProjet.Application.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanqueProjet.Web.Areas.BanqueProjet.Controllers;

/// <summary>Banque de projets : projets dont les études de projet ont été validées par le MPCE.</summary>
[Area("BanqueProjet")]
[Authorize]
public class ProjetsController : Controller
{
    private readonly IProjetValideRepository _repo;
    public ProjetsController(IProjetValideRepository repo) => _repo = repo;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _repo.ListerAsync(ct));

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var p = await _repo.ObtenirAsync(id, ct);
        return p is null ? NotFound() : View(p);
    }
}

/// <summary>Point d'entrée du module (lien du menu latéral).</summary>
[Area("BanqueProjet")]
[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Projets");
}
