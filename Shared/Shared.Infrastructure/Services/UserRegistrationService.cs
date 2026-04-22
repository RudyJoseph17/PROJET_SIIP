using Microsoft.AspNetCore.Identity;
using Shared.Domain.Dtos;
using Shared.Domain.Entities;
using Shared.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Services
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPasswordGenerator _passwordGenerator;

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IPasswordGenerator passwordGenerator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _passwordGenerator = passwordGenerator;
        }

        public async Task<(bool Success, string GeneratedPassword, IEnumerable<string> Errors)>
            RegisterAsync(RegisterUserRequest request)
        {
            var password = _passwordGenerator.Generate();

            var user = new ApplicationUser
            {
                UserName = request.NomUtilisateur,
                Nom = request.Nom,
                Prenom = request.Prenom,
                Ministere = request.Ministere,
                Entite = request.Entite,
                //Privilege = request.Privilege,
                MustChangePassword = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                return (false, null, result.Errors.Select(e => e.Description));

            if (!await _roleManager.RoleExistsAsync(request.Role))
                await _roleManager.CreateAsync(new IdentityRole(request.Role));

            await _userManager.AddToRoleAsync(user, request.Role);

            return (true, password, Enumerable.Empty<string>());
        }
    }
}
