using InvestissementsPublics.Starter.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Domain.Entities;
using Shared.Infrastructure;

public class RoleController : Controller
{
//    private readonly UserManager<IdentityUser> _userManager;
//    private readonly RoleManager<IdentityRole> _roleManager;
//    private readonly ApplicationDbContext _context;

//    public RoleController(
//        UserManager<IdentityUser> userManager,
//        RoleManager<IdentityRole> roleManager,
//        ApplicationDbContext context)
//    {
//        _userManager = userManager;
//        _roleManager = roleManager;
//        _context = context;
//    }

//    // PAGE UNIQUE
//    public IActionResult Admin()
//    {
//        var vm = new AdminRolesViewModel
//        {
//            Users = _userManager.Users.ToList(),
//            Roles = _roleManager.Roles.ToList(),
//            Privileges = _context.Privileges.ToList()
//        };

//        return View(vm);
//    }

//    // USER → ROLE
//    [HttpPost]
//    public async Task<IActionResult> AssignUserToRole(AdminRolesViewModel model)
//    {
//        if (string.IsNullOrEmpty(model.SelectedUserId) || string.IsNullOrEmpty(model.SelectedRoleName))
//            return BadRequest("Paramètres invalides");

//        var user = await _userManager.FindByIdAsync(model.SelectedUserId);
//        if (user == null)
//            return NotFound();

//        await _userManager.AddToRoleAsync(user, model.SelectedRoleName);

//        return RedirectToAction("Admin");
//    }

//    // ROLE → PRIVILEGES
//    [HttpPost]
//    public async Task<IActionResult> AssignPrivilegesToRole(AdminRolesViewModel model)
//    {
//        var existing = _context.RolePrivileges
//            .Where(x => x.IdRole == model.SelectedRoleId);

//        _context.RolePrivileges.RemoveRange(existing);

//        foreach (var p in model.SelectedPrivileges)
//        {
//            _context.RolePrivileges.Add(new RolePrivilege
//            {
//                IdRole = model.SelectedRoleId,
//                IdPrivilege = p
//            });
//        }

//        await _context.SaveChangesAsync();

//        return RedirectToAction("Admin");
//    }

//    public IActionResult CreateRole()
//    {
//        return View();
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreateRole(string roleName)
//    {
//        if (string.IsNullOrEmpty(roleName))
//        {
//            ModelState.AddModelError("", "Nom du rôle requis");
//            return View();
//        }

//        var exists = await _roleManager.RoleExistsAsync(roleName);
//        if (exists)
//        {
//            ModelState.AddModelError("", "Ce rôle existe déjà");
//            return View();
//        }

//        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

//        if (!result.Succeeded)
//        {
//            foreach (var error in result.Errors)
//                ModelState.AddModelError("", error.Description);

//            return View();
//        }

//        return RedirectToAction("Admin");
//    }

//    public IActionResult CreatePrivilege()
//    {
//        return View();
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreatePrivilege(string name)
//    {
//        if (string.IsNullOrEmpty(name))
//        {
//            ModelState.AddModelError("", "Nom du privilège requis");
//            return View();
//        }

//        var exists = _context.Privileges.Any(p => p.NomPrivilege == name);
//        if (exists)
//        {
//            ModelState.AddModelError("", "Ce privilège existe déjà");
//            return View();
//        }

//        _context.Privileges.Add(new Privilege
//        {
//            NomPrivilege = name
//        });

//        await _context.SaveChangesAsync();

//        return RedirectToAction("Admin");
//    }

}