using InvestissementsPublics.Starter.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Shared.Domain.Entities;
using Shared.Infrastructure;
using Shared.Domain.Interfaces;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordGenerator _passwordGenerator;

        public RoleController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context,
            IPasswordGenerator passwordGenerator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _passwordGenerator = passwordGenerator;
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
                string.IsNullOrWhiteSpace(model.NewUserMinistere))
            {
                TempData["Error"] = "Nom d'utilisateur, email et ministère sont obligatoires.";
                return RedirectToAction(nameof(Admin));
            }

            var generatedPassword = _passwordGenerator.Generate();

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

            var result = await _userManager.CreateAsync(user, generatedPassword);
            if (result.Succeeded)
            {
                var roleResult = await ReplaceUserRolesAsync(user, model.NewUserRoleIds);
                TempData[roleResult.Succeeded ? "Success" : "Error"] = roleResult.Succeeded
                    ? $"Utilisateur ajouté avec succès. Mot de passe généré : {generatedPassword}"
                    : string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            }

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
            if (result.Succeeded)
            {
                var roleResult = await ReplaceUserRolesAsync(user, model.EditUserRoleIds);
                TempData[roleResult.Succeeded ? "Success" : "Error"] = roleResult.Succeeded
                    ? "Utilisateur et rôles modifiés avec succès."
                    : string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            }

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
            if (result.Succeeded)
            {
                await ReplaceRolePrivilegesAsync(role.Id, model.SelectedPrivileges);
                TempData["Success"] = "Rôle et privilèges associés modifiés avec succès.";
            }
            else
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
            }

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

            var exists = (await _context.Privileges.CountAsync(p => p.NomPrivilege == name)) > 0;
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

            await ReplaceRolePrivilegesAsync(model.RoleIdForPrivileges, model.SelectedPrivileges);

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
                RolePrivilegeIds = rolePrivileges
                    .GroupBy(rp => rp.RoleId)
                    .ToDictionary(g => g.Key, g => g.Select(rp => rp.IdPrivilege).Distinct().ToList()),
            };

            foreach (var user in model.Users)
            {
                var roleNames = await _userManager.GetRolesAsync(user);
                model.UserRoleIds[user.Id] = model.Roles
                    .Where(role => role.Name is not null && roleNames.Contains(role.Name))
                    .Select(role => role.Id)
                    .ToList();
            }

            return model;
        }
    private async Task<IdentityResult> ReplaceUserRolesAsync(ApplicationUser user, IEnumerable<string>? selectedRoleIds)
    {
        var selectedRoleIdList = (selectedRoleIds ?? Enumerable.Empty<string>())
            .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
            .Distinct()
            .ToList();

        var selectedRoleNames = await _roleManager.Roles
            .Where(role => selectedRoleIdList.Contains(role.Id) && role.Name != null)
            .Select(role => role.Name!)
            .ToListAsync();

        var currentRoleNames = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoleNames);
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        return selectedRoleNames.Count == 0
            ? IdentityResult.Success
            : await _userManager.AddToRolesAsync(user, selectedRoleNames);
    }

    private async Task ReplaceRolePrivilegesAsync(string roleId, IEnumerable<int>? selectedPrivileges)
    {
        var selectedPrivilegeIds = (selectedPrivileges ?? Enumerable.Empty<int>())
            .Distinct()
            .ToList();

        var existing = _context.RolePrivileges.Where(x => x.RoleId == roleId);
        _context.RolePrivileges.RemoveRange(existing);

        var validPrivilegeIds = await _context.Privileges
            .Where(privilege => selectedPrivilegeIds.Contains(privilege.IdPrivilege))
            .Select(privilege => privilege.IdPrivilege)
            .ToListAsync();

        foreach (var privilegeId in validPrivilegeIds.Distinct())
        {
            _context.RolePrivileges.Add(new RolePrivilege
            {
                RoleId = roleId,
                IdPrivilege = privilegeId
            });
        }

        await _context.SaveChangesAsync();
    }
}
}