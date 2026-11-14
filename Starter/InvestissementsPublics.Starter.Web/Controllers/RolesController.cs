using InvestissementsPublics.Starter.Web.Models.Securite;
using InvestissementsPublics.Starter.Web.Securite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Infrastructure;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    /// <summary>Sécurité — gestion des rôles et de leurs privilèges.</summary>
    [Authorize(Roles = RolesSecurite.Administrateurs)]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager,
                               ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        // GET /Roles
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
            var userRoles = await _context.UserRoles.AsNoTracking().ToListAsync();
            var rolePrivileges = await _context.RolePrivileges.AsNoTracking().ToListAsync();

            return View(roles.Select(r => new RoleLigneViewModel
            {
                Id = r.Id,
                Nom = r.Name ?? "",
                NbUtilisateurs = userRoles.Count(ur => ur.RoleId == r.Id),
                NbPrivileges = rolePrivileges.Count(rp => rp.RoleId == r.Id),
                EstSysteme = RolesSecurite.EstSysteme(r.Name),
            }).ToList());
        }

        // GET /Roles/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            var privilegeIds = await _context.RolePrivileges.Where(rp => rp.RoleId == id).Select(rp => rp.IdPrivilege).ToListAsync();
            return View(new RoleDetailsViewModel
            {
                Role = role,
                Privileges = await _context.Privileges.AsNoTracking().Where(p => privilegeIds.Contains(p.IdPrivilege))
                                           .OrderBy(p => p.NomPrivilege).ToListAsync(),
                Utilisateurs = (await _userManager.GetUsersInRoleAsync(role.Name ?? "")).OrderBy(u => u.UserName).ToList(),
                EstSysteme = RolesSecurite.EstSysteme(role.Name),
            });
        }

        // GET /Roles/Create
        public async Task<IActionResult> Create() => View(await AvecPrivilegesAsync(new RoleFormViewModel()));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleFormViewModel model)
        {
            var nom = model.Nom?.Trim() ?? "";
            if (ModelState.IsValid && await _roleManager.RoleExistsAsync(nom))
                ModelState.AddModelError(nameof(model.Nom), "Ce rôle existe déjà.");
            if (!ModelState.IsValid) return View(await AvecPrivilegesAsync(model));

            var role = new ApplicationRole { Name = nom };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(await AvecPrivilegesAsync(model));
            }

            await RemplacerPrivilegesAsync(role.Id, model.PrivilegeIds);
            TempData["Success"] = $"Rôle « {nom} » créé.";
            return RedirectToAction(nameof(Details), new { id = role.Id });
        }

        // GET /Roles/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();
            return View(await AvecPrivilegesAsync(new RoleFormViewModel
            {
                Id = role.Id,
                Nom = role.Name ?? "",
                PrivilegeIds = await _context.RolePrivileges.Where(rp => rp.RoleId == id).Select(rp => rp.IdPrivilege).ToListAsync(),
            }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, RoleFormViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();
            model.Id = id;

            var systeme = RolesSecurite.EstSysteme(role.Name);
            var nom = systeme ? role.Name! : model.Nom?.Trim() ?? "";
            if (systeme) ModelState.Remove(nameof(model.Nom));
            if (ModelState.IsValid && !string.Equals(nom, role.Name, StringComparison.OrdinalIgnoreCase)
                && await _roleManager.RoleExistsAsync(nom))
                ModelState.AddModelError(nameof(model.Nom), "Ce rôle existe déjà.");
            if (!ModelState.IsValid) return View(await AvecPrivilegesAsync(model));

            if (!systeme && nom != role.Name)
            {
                role.Name = nom;
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(await AvecPrivilegesAsync(model));
                }
            }

            await RemplacerPrivilegesAsync(role.Id, model.PrivilegeIds);
            TempData["Success"] = "Rôle modifié.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Roles/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();
            if (RolesSecurite.EstSysteme(role.Name))
            {
                TempData["Error"] = $"Le rôle « {role.Name} » est un rôle système : il ne peut pas être supprimé.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.RolePrivileges.RemoveRange(_context.RolePrivileges.Where(rp => rp.RoleId == id));
            await _context.SaveChangesAsync();

            var result = await _roleManager.DeleteAsync(role);   // retire aussi le rôle aux utilisateurs
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id });
            }
            TempData["Success"] = $"Rôle « {role.Name} » supprimé.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------ utilitaires

        private async Task<RoleFormViewModel> AvecPrivilegesAsync(RoleFormViewModel model)
        {
            model.PrivilegesDisponibles = await _context.Privileges.AsNoTracking().OrderBy(p => p.NomPrivilege).ToListAsync();
            if (model.Id is not null)
            {
                var role = await _roleManager.FindByIdAsync(model.Id);
                model.EstSysteme = RolesSecurite.EstSysteme(role?.Name);
                if (model.EstSysteme) model.Nom = role!.Name!;
            }
            return model;
        }

        private async Task RemplacerPrivilegesAsync(string roleId, IEnumerable<int>? privilegeIds)
        {
            var voulus = (privilegeIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            var valides = await _context.Privileges.Where(p => voulus.Contains(p.IdPrivilege)).Select(p => p.IdPrivilege).ToListAsync();

            _context.RolePrivileges.RemoveRange(_context.RolePrivileges.Where(rp => rp.RoleId == roleId));
            foreach (var idPrivilege in valides)
                _context.RolePrivileges.Add(new RolePrivilege { RoleId = roleId, IdPrivilege = idPrivilege });
            await _context.SaveChangesAsync();
        }
    }
}
