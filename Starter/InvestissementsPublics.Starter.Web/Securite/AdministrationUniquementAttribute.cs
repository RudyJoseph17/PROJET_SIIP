using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvestissementsPublics.Starter.Web.Securite
{
    /// <summary>
    /// Réserve un contrôleur aux rôles Admin et Gestionnaire système, sans révéler son existence :
    /// visiteur non connecté -> page de connexion ; utilisateur connecté sans ces rôles -> 404.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AdministrationUniquementAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) return;

            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }
            if (!user.IsInRole(RolesSecurite.Admin) && !user.IsInRole(RolesSecurite.GestionnaireSysteme))
                context.Result = new NotFoundResult();
        }
    }
}
