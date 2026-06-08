using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Entities;
using Shared.Infrastructure;

namespace InvestissementsPublics.Starter.Web.Data
{
    public class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
            var db = sp.GetRequiredService<ApplicationDbContext>();

            // =========================
            // 1. ROLES
            // =========================
            var roles = new[] { "Gestionnaire systeme", "Admin" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    };

                    var res = await roleManager.CreateAsync(role);

                    if (!res.Succeeded)
                        Console.WriteLine($"Erreur rôle '{roleName}': {string.Join(", ", res.Errors.Select(e => e.Description))}");
                    else
                        Console.WriteLine($"Rôle créé: {roleName}");
                }
            }

            // =========================
            // 2. USER ADMIN
            // =========================
            var adminUserName = "rudyjoseph";
            var adminEmail = "admin@site.com";

            var admin = await userManager.FindByNameAsync(adminUserName);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminUserName,
                    NomUtilisateur = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Nom = "Joseph",
                    Prenom = "Rudy",
                    Ministere = "MPCE",
                    Entite = "Direction Generale",
                    MustChangePassword = false
                };

                var createRes = await userManager.CreateAsync(admin, "Admin@123");

                if (!createRes.Succeeded)
                {
                    Console.WriteLine($"Erreur création admin: {string.Join(", ", createRes.Errors.Select(e => e.Description))}");
                    return;
                }

                Console.WriteLine("✅ Utilisateur admin créé");
            }

            // =========================
            // 3. PRIVILEGES
            // =========================
            var privileges = new[]
            {
                new Privilege { NomPrivilege = "BanqueProjet.Projet.Create", DescriptionPrivilege = "Créer un projet BanqueProjet" },
                new Privilege { NomPrivilege = "BanqueProjet.Projet.Edit", DescriptionPrivilege = "Modifier un projet BanqueProjet" },
                new Privilege { NomPrivilege = "Programmation.Prevision.Approve", DescriptionPrivilege = "Approuver une prévision" }
            };

            using var tx = await db.Database.BeginTransactionAsync();

            try
            {
                var roleSystem = await roleManager.FindByNameAsync("Gestionnaire systeme");

                foreach (var p in privileges)
                {
                    var privilege = await db.Privileges
                        .FirstOrDefaultAsync(x => x.NomPrivilege == p.NomPrivilege);

                    if (privilege == null)
                    {
                        privilege = new Privilege
                        {
                            NomPrivilege = p.NomPrivilege,
                            DescriptionPrivilege = p.DescriptionPrivilege
                        };

                        db.Privileges.Add(privilege);
                        await db.SaveChangesAsync();

                        Console.WriteLine($"Privilege ajouté: {p.NomPrivilege}");
                    }

                    // =========================
                    // 4. ROLE -> PRIVILEGE LINK
                    // =========================
                    if (roleSystem != null)
                    {
                        var exists = (await db.RolePrivileges.CountAsync(rp =>
                        rp.RoleId == roleSystem.Id &&
                        rp.IdPrivilege == privilege.IdPrivilege)) > 0;

                        if (!exists)
                        {
                            db.RolePrivileges.Add(new RolePrivilege
                            {
                                RoleId = roleSystem.Id,
                                IdPrivilege = privilege.IdPrivilege
                            });

                            Console.WriteLine($"Lien RolePrivilege: {roleSystem.Name} -> {privilege.NomPrivilege}");
                        }
                    }
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur seed privileges: {ex.Message}");
                await tx.RollbackAsync();
            }

            // =========================
            // 5. ROLE ASSIGNMENT
            // =========================
            if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var res = await userManager.AddToRoleAsync(admin, "Admin");

                if (res.Succeeded)
                    Console.WriteLine("✅ Admin ajouté au rôle Admin");
                else
                    Console.WriteLine($"Erreur rôle admin: {string.Join(", ", res.Errors.Select(e => e.Description))}");
            }

            Console.WriteLine("Seed Identity terminé.");
        }
    }
}