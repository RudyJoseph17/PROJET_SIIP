using InvestissementsPublics.Starter.Web.Models.Securite;
using InvestissementsPublics.Starter.Web.Securite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Domain.Interfaces;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    /// <summary>Sécurité — gestion des utilisateurs.</summary>
    [AdministrationUniquement]
    public class UtilisateursController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IPasswordGenerator _passwordGenerator;

        public UtilisateursController(UserManager<ApplicationUser> userManager,
                                      RoleManager<ApplicationRole> roleManager,
                                      IPasswordGenerator passwordGenerator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _passwordGenerator = passwordGenerator;
        }

        // GET /Utilisateurs
        public async Task<IActionResult> Index()
        {
            var lignes = await _userManager.Users.AsNoTracking()
                .OrderBy(u => u.UserName)
                .Select(u => new UtilisateurLigneViewModel
                {
                    Id = u.Id,
                    NomUtilisateur = u.UserName,
                    NomComplet = (u.Prenom + " " + u.Nom).Trim(),
                    Ministere = u.Ministere,
                    Entite = u.Entite,
                })
                .ToListAsync();
            return View(lignes);
        }

        // GET /Utilisateurs/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();
            return View(new UtilisateurDetailsViewModel
            {
                Utilisateur = user,
                Roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList(),
                EstUtilisateurCourant = user.Id == _userManager.GetUserId(User),
            });
        }

        // GET /Utilisateurs/Create — « Nouvel utilisateur »
        public async Task<IActionResult> Create() =>
            View(await AvecRolesAsync(new UtilisateurFormViewModel()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UtilisateurFormViewModel model)
        {
            if (!ModelState.IsValid) return View(await AvecRolesAsync(model));

            var motDePasse = _passwordGenerator.Generate();
            var user = new ApplicationUser
            {
                UserName = model.NomUtilisateur.Trim(),
                NomUtilisateur = model.NomUtilisateur.Trim(),
                Email = model.Email.Trim(),
                Nom = model.Nom.Trim(),
                Prenom = model.Prenom.Trim(),
                Ministere = model.Ministere.Trim(),
                Entite = model.Entite.Trim(),
                EmailConfirmed = true,
            };

            var result = await _userManager.CreateAsync(user, motDePasse);
            if (result.Succeeded) result = await RemplacerRolesAsync(user, model.RoleIds);
            if (!result.Succeeded)
            {
                AjouterErreurs(result);
                return View(await AvecRolesAsync(model));
            }

            TempData["Success"] = $"Utilisateur « {user.UserName} » créé. Mot de passe provisoire : {motDePasse} "
                                  + "(à communiquer à l'utilisateur, qui devra le changer à la première connexion).";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        // GET /Utilisateurs/Edit/{id} — « Modification utilisateur »
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var nomsRoles = await _userManager.GetRolesAsync(user);
            var model = new UtilisateurFormViewModel
            {
                Id = user.Id,
                NomUtilisateur = user.UserName ?? "",
                Email = user.Email ?? "",
                Nom = user.Nom ?? "",
                Prenom = user.Prenom ?? "",
                Ministere = user.Ministere ?? "",
                Entite = user.Entite ?? "",
                RoleIds = await _roleManager.Roles.Where(r => r.Name != null && nomsRoles.Contains(r.Name))
                                                  .Select(r => r.Id).ToListAsync(),
            };
            return View(await AvecRolesAsync(model));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UtilisateurFormViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();
            model.Id = id;
            if (!ModelState.IsValid) return View(await AvecRolesAsync(model));

            // On ne retire pas le rôle Admin au dernier administrateur
            var adminRole = await _roleManager.FindByNameAsync(RolesSecurite.Admin);
            if (adminRole is not null && !model.RoleIds.Contains(adminRole.Id)
                && await EstDernierAdministrateurAsync(user))
            {
                ModelState.AddModelError(nameof(model.RoleIds),
                    "Impossible de retirer le rôle Admin : c'est le dernier administrateur.");
                return View(await AvecRolesAsync(model));
            }

            user.UserName = model.NomUtilisateur.Trim();
            user.NomUtilisateur = model.NomUtilisateur.Trim();
            user.Email = model.Email.Trim();
            user.Nom = model.Nom.Trim();
            user.Prenom = model.Prenom.Trim();
            user.Ministere = model.Ministere.Trim();
            user.Entite = model.Entite.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) result = await RemplacerRolesAsync(user, model.RoleIds);
            if (!result.Succeeded)
            {
                AjouterErreurs(result);
                return View(await AvecRolesAsync(model));
            }

            TempData["Success"] = "Utilisateur modifié.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Utilisateurs/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Vous ne pouvez pas supprimer votre propre compte.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (await EstDernierAdministrateurAsync(user))
            {
                TempData["Error"] = "Impossible de supprimer le dernier administrateur.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }
            TempData["Success"] = $"Utilisateur « {user.UserName} » supprimé.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------ utilitaires

        private async Task<UtilisateurFormViewModel> AvecRolesAsync(UtilisateurFormViewModel model)
        {
            model.RolesDisponibles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
            return model;
        }

        private async Task<bool> EstDernierAdministrateurAsync(ApplicationUser user) =>
            await _userManager.IsInRoleAsync(user, RolesSecurite.Admin)
            && (await _userManager.GetUsersInRoleAsync(RolesSecurite.Admin)).Count <= 1;

        private async Task<IdentityResult> RemplacerRolesAsync(ApplicationUser user, IEnumerable<string>? roleIds)
        {
            var ids = (roleIds ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var voulus = await _roleManager.Roles.Where(r => ids.Contains(r.Id) && r.Name != null)
                                                 .Select(r => r.Name!).ToListAsync();
            var actuels = await _userManager.GetRolesAsync(user);

            var aRetirer = actuels.Except(voulus).ToList();
            var aAjouter = voulus.Except(actuels).ToList();
            if (aRetirer.Count > 0)
            {
                var r = await _userManager.RemoveFromRolesAsync(user, aRetirer);
                if (!r.Succeeded) return r;
            }
            return aAjouter.Count > 0 ? await _userManager.AddToRolesAsync(user, aAjouter) : IdentityResult.Success;
        }

        private void AjouterErreurs(IdentityResult result)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        }
    }
}
