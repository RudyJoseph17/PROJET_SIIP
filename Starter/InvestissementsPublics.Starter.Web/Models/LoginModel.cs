using Microsoft.AspNetCore.Identity;
using Shared.Domain.Entities;

namespace InvestissementsPublics.Starter.Web.Models
{
    public class LoginModel
    {

        // Propriétés pour le formulaire de login
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; }
        }
    }