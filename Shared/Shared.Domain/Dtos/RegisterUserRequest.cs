using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Dtos
{
    public class RegisterUserRequest
    {
        [Required] public string Nom { get; set; }
        [Required] public string Prenom { get; set; }
        [Required] public string Ministere { get; set; }
        [Required] public string Entite { get; set; }
        [Required] public string Role { get; set; }
        [Required] public string Privilege { get; set; }
        [Required] public string NomUtilisateur  { get; set; }
    }
}
