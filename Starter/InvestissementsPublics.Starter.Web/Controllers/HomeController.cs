using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InvestissementsPublics.Starter.Web.Models;
using Shared.Domain.Entities;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // ======================
        // GET: Home/Index
        // ======================
        [HttpGet]
        public IActionResult Index()
        {
            // On envoie un modèle vide (ViewModel simple)
            return View(new LoginModel());
        }

        // ======================
        // POST: Home/Index (Login)
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Vérification des champs
            if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Tous les champs sont obligatoires.");
                return View(model);
            }

            // Recherche utilisateur
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                ModelState.AddModelError("", "Utilisateur introuvable.");
                return View(model);
            }

            // Tentative de connexion
            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                if (user.MustChangePassword)
                {
                    return RedirectToPage("/Account/Manage/ChangePassword", new { area = "Identity" });
                }
                // Redirection après succès
                return RedirectToAction(
                                          "Index",
                                          "Home",
                                          new { area = "TableauxDeBord" }
                                        );
            }

            // Échec login
            ModelState.AddModelError("", "Nom d'utilisateur ou mot de passe incorrect.");
            return View(model);
        }

        // ======================
        // POST: Home/Logout
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index), "Home", new { area = "" });
        }

        // ======================
        // Page sécurisée (exemple)
        // ======================
        public IActionResult Privacy()
        {
            return View();
        }

        // ======================
        // Gestion erreurs
        // ======================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}