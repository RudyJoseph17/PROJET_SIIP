using InvestissementsPublics.Starter.Web.Models.Securite;
using InvestissementsPublics.Starter.Web.Securite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Infrastructure;

namespace InvestissementsPublics.Starter.Web.Controllers
{
    /// <summary>Sécurité — gestion des privilèges (attribués aux rôles depuis la page Rôles).</summary>
    [AdministrationUniquement]
    public class PrivilegesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrivilegesController(ApplicationDbContext context) => _context = context;

        // GET /Privileges
        public async Task<IActionResult> Index()
        {
            var privileges = await _context.Privileges.AsNoTracking().OrderBy(p => p.NomPrivilege).ToListAsync();
            var liens = await _context.RolePrivileges.AsNoTracking().ToListAsync();
            return View(privileges.Select(p => new PrivilegeLigneViewModel
            {
                Id = p.IdPrivilege,
                Nom = p.NomPrivilege,
                Description = p.DescriptionPrivilege,
                NbRoles = liens.Count(l => l.IdPrivilege == p.IdPrivilege),
            }).ToList());
        }

        // GET /Privileges/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var privilege = await _context.Privileges.AsNoTracking().FirstOrDefaultAsync(p => p.IdPrivilege == id);
            if (privilege is null) return NotFound();

            var roleIds = await _context.RolePrivileges.Where(rp => rp.IdPrivilege == id).Select(rp => rp.RoleId).ToListAsync();
            return View(new PrivilegeDetailsViewModel
            {
                Privilege = privilege,
                Roles = await _context.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).OrderBy(r => r.Name).ToListAsync(),
            });
        }

        // GET /Privileges/Create
        public IActionResult Create() => View(new PrivilegeFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PrivilegeFormViewModel model)
        {
            var nom = model.Nom?.Trim() ?? "";
            if (ModelState.IsValid && await ExisteAsync(nom, null))
                ModelState.AddModelError(nameof(model.Nom), "Ce privilège existe déjà.");
            if (!ModelState.IsValid) return View(model);

            var privilege = new Privilege { NomPrivilege = nom, DescriptionPrivilege = model.Description?.Trim() ?? string.Empty };
            _context.Privileges.Add(privilege);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Privilège « {nom} » créé. Attribuez-le aux rôles depuis la page Rôles.";
            return RedirectToAction(nameof(Details), new { id = privilege.IdPrivilege });
        }

        // GET /Privileges/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _context.Privileges.AsNoTracking().FirstOrDefaultAsync(x => x.IdPrivilege == id);
            if (p is null) return NotFound();
            return View(new PrivilegeFormViewModel { Id = p.IdPrivilege, Nom = p.NomPrivilege, Description = p.DescriptionPrivilege });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PrivilegeFormViewModel model)
        {
            var privilege = await _context.Privileges.FindAsync(id);
            if (privilege is null) return NotFound();
            model.Id = id;

            var nom = model.Nom?.Trim() ?? "";
            if (ModelState.IsValid && await ExisteAsync(nom, id))
                ModelState.AddModelError(nameof(model.Nom), "Ce privilège existe déjà.");
            if (!ModelState.IsValid) return View(model);

            privilege.NomPrivilege = nom;
            privilege.DescriptionPrivilege = model.Description?.Trim() ?? string.Empty;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Privilège modifié.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Privileges/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var privilege = await _context.Privileges.FindAsync(id);
            if (privilege is null) return NotFound();

            _context.RolePrivileges.RemoveRange(_context.RolePrivileges.Where(rp => rp.IdPrivilege == id));
            _context.Privileges.Remove(privilege);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Privilège « {privilege.NomPrivilege} » supprimé.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ExisteAsync(string nom, int? saufId)
        {
            var n = nom.ToUpper();
            return await _context.Privileges.CountAsync(p => p.NomPrivilege.ToUpper() == n
                                                          && (saufId == null || p.IdPrivilege != saufId)) > 0;
        }
    }
}
