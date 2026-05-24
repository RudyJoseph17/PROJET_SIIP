using InvestissementsPublics.Starter.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Shared.Domain.Entities;
using Shared.Infrastructure;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public RoleController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            var model = await BuildAdminViewModelAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(AdminRolesViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.NewUserName) ||
                string.IsNullOrWhiteSpace(model.NewUserEmail) ||
                string.IsNullOrWhiteSpace(model.NewUserPassword))
            {
                TempData["Error"] = "Nom d'utilisateur, email et mot de passe sont obligatoires.";
                return RedirectToAction(nameof(Admin));
            }

            var user = new ApplicationUser
            {
                UserName = model.NewUserName.Trim(),
                NomUtilisateur = model.NewUserName.Trim(),
                Email = model.NewUserEmail.Trim(),
                Nom = model.NewUserNom?.Trim() ?? string.Empty,
                Prenom = model.NewUserPrenom?.Trim() ?? string.Empty,
                Ministere = model.NewUserMinistere?.Trim() ?? string.Empty,
                Entite = model.NewUserEntite?.Trim() ?? string.Empty,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.NewUserPassword);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Utilisateur ajouté avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(AdminRolesViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EditUserId))
            {
                TempData["Error"] = "Utilisateur introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var user = await _userManager.FindByIdAsync(model.EditUserId);
            if (user is null)
            {
                TempData["Error"] = "Utilisateur introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            user.UserName = model.EditUserName.Trim();
            user.NomUtilisateur = model.EditUserName.Trim();
            user.Email = model.EditUserEmail.Trim();
            user.Nom = model.EditUserNom?.Trim() ?? string.Empty;
            user.Prenom = model.EditUserPrenom?.Trim() ?? string.Empty;
            user.Ministere = model.EditUserMinistere?.Trim() ?? string.Empty;
            user.Entite = model.EditUserEntite?.Trim() ?? string.Empty;

            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Utilisateur modifié avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                TempData["Error"] = "Utilisateur introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Utilisateur supprimé avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(AdminRolesViewModel model)
        {
            var name = model.NewRoleName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Le nom du rôle est obligatoire.";
                return RedirectToAction(nameof(Admin));
            }

            if (await _roleManager.RoleExistsAsync(name))
            {
                TempData["Error"] = "Ce rôle existe déjà.";
                return RedirectToAction(nameof(Admin));
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole { Name = name });
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Rôle ajouté avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(AdminRolesViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.EditRoleId) || string.IsNullOrWhiteSpace(model.EditRoleName))
            {
                TempData["Error"] = "Informations du rôle invalides.";
                return RedirectToAction(nameof(Admin));
            }

            var role = await _roleManager.FindByIdAsync(model.EditRoleId);
            if (role is null)
            {
                TempData["Error"] = "Rôle introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            role.Name = model.EditRoleName.Trim();
            role.NormalizedName = model.EditRoleName.Trim().ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(role);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Rôle modifié avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role is null)
            {
                TempData["Error"] = "Rôle introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var rolePrivileges = _context.RolePrivileges.Where(rp => rp.RoleId == roleId);
            _context.RolePrivileges.RemoveRange(rolePrivileges);
            await _context.SaveChangesAsync();

            var result = await _roleManager.DeleteAsync(role);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Rôle supprimé avec succès."
                : string.Join(" | ", result.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePrivilege(AdminRolesViewModel model)
        {
            var name = model.NewPrivilegeName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Le nom du privilège est obligatoire.";
                return RedirectToAction(nameof(Admin));
            }

            var exists = await _context.Privileges.AnyAsync(p => p.NomPrivilege == name);
            if (exists)
            {
                TempData["Error"] = "Ce privilège existe déjà.";
                return RedirectToAction(nameof(Admin));
            }

            _context.Privileges.Add(new Privilege
            {
                NomPrivilege = name,
                DescriptionPrivilege = model.NewPrivilegeDescription?.Trim() ?? string.Empty
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Privilège ajouté avec succès.";
            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrivilege(AdminRolesViewModel model)
        {
            if (model.EditPrivilegeId is null)
            {
                TempData["Error"] = "Privilège introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var privilege = await _context.Privileges.FindAsync(model.EditPrivilegeId.Value);
            if (privilege is null)
            {
                TempData["Error"] = "Privilège introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            privilege.NomPrivilege = model.EditPrivilegeName.Trim();
            privilege.DescriptionPrivilege = model.EditPrivilegeDescription?.Trim() ?? string.Empty;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Privilège modifié avec succès.";

            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePrivilege(int privilegeId)
        {
            var privilege = await _context.Privileges.FindAsync(privilegeId);
            if (privilege is null)
            {
                TempData["Error"] = "Privilège introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var links = _context.RolePrivileges.Where(rp => rp.IdPrivilege == privilegeId);
            _context.RolePrivileges.RemoveRange(links);
            _context.Privileges.Remove(privilege);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Privilège supprimé avec succès.";
            return RedirectToAction(nameof(Admin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRolePrivileges(AdminRolesViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.RoleIdForPrivileges))
            {
                TempData["Error"] = "Veuillez choisir un rôle.";
                return RedirectToAction(nameof(Admin));
            }

            var role = await _roleManager.FindByIdAsync(model.RoleIdForPrivileges);
            if (role is null)
            {
                TempData["Error"] = "Rôle introuvable.";
                return RedirectToAction(nameof(Admin));
            }

            var existing = _context.RolePrivileges.Where(x => x.RoleId == model.RoleIdForPrivileges);
            _context.RolePrivileges.RemoveRange(existing);

            foreach (var privilegeId in model.SelectedPrivileges.Distinct())
            {
                _context.RolePrivileges.Add(new RolePrivilege
                {
                    RoleId = model.RoleIdForPrivileges,
                    IdPrivilege = privilegeId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Privilèges du rôle mis à jour.";

            return RedirectToAction(nameof(Admin));
        }

        private async Task<AdminRolesViewModel> BuildAdminViewModelAsync()
        {
            var rolePrivileges = await _context.RolePrivileges
                .AsNoTracking()
                .ToListAsync();

            var model = new AdminRolesViewModel
            {
                Users = await _userManager.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync(),
                Roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(),
                Privileges = await _context.Privileges.AsNoTracking().OrderBy(p => p.NomPrivilege).ToListAsync(),
                SelectedPrivileges = rolePrivileges.Select(rp => rp.IdPrivilege).Distinct().ToList()
            };

            return model;
        }
    }
}