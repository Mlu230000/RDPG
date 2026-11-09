using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters
{
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _requiredRole;

        public AuthorizeRoleAttribute(string requiredRole)
        {
            _requiredRole = requiredRole;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var role = context.HttpContext.Session.GetString("Role");
            var token = context.HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(role))
            {
                // Unauthenticated user -> redirect to Login
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            if (!string.Equals(role, _requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                // Role mismatch -> redirect to appropriate home or access denied
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            }
        }
    }

    public class AuthorizeOrganiserAttribute : AuthorizeRoleAttribute
    {
        public AuthorizeOrganiserAttribute() : base("Organiser") { }
    }

    public class AuthorizeParticipantAttribute : AuthorizeRoleAttribute
    {
        public AuthorizeParticipantAttribute() : base("Participant") { }
    }
}
