using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VectorRagDemo.Filters
{
    /// <summary>
    /// Globale filter die ingelogde gebruikers zonder actieve toestemming
    /// doorverwijst naar de toestemmingspagina.
    /// Slaat requests naar de Account-controller over zodat inloggen en
    /// de toestemmingspagina zelf altijd bereikbaar blijven.
    /// </summary>
    public class ConsentRequiredFilter : IAsyncResourceFilter
    {
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated == true
                && user.HasClaim("ConsentRequired", "true"))
            {
                var controller = context.RouteData.Values["controller"]?.ToString();

                // Account-routes (Login, Logout, Toestemming, enz.) nooit blokkeren
                if (!string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectToActionResult("Toestemming", "Account", null);
                    return;
                }
            }

            await next();
        }
    }
}
