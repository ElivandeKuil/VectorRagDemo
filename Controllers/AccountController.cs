using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserAuthenticationService _authService;
        private readonly ProjectAccessService _projectAccessService;

        public AccountController(UserAuthenticationService authService, ProjectAccessService projectAccessService)
        {
            _authService = authService;
            _projectAccessService = projectAccessService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.ValidateUserAsync(model.GebruikersNaam, model.Wachtwoord);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Ongeldige gebruikersnaam of wachtwoord.");
                return View(model);
            }

            var roles = await _authService.GetUserRolesAsync(user.ID);
            var subProjectNames = await _projectAccessService.GetUserSubProjectNamesAsync(user.ID);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.GebruikersNaam),
                new Claim("FullName", user.Naam)
            };

            // Prompt user to change their password when the admin has flagged it
            if (user.WachtwoordWijzigen)
                claims.Add(new Claim("MustChangePassword", "true"));

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            // Store sub-project access as claims so the layout can filter nav items
            // without a DB query on every request. Users must re-login after access changes.
            foreach (var subProject in subProjectNames)
                claims.Add(new Claim("SubProject", subProject));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.GetUserId();
            var success = await _authService.ChangePasswordAsync(userId, model.HuidigWachtwoord, model.NieuwWachtwoord);

            if (!success)
            {
                ModelState.AddModelError(nameof(model.HuidigWachtwoord), "Het huidige wachtwoord is onjuist.");
                return View(model);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Wachtwoord succesvol gewijzigd. Log opnieuw in om door te gaan.";
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Utility endpoint to generate password hashes for initial user setup.
        /// Only available in Development environment.
        /// Usage: /Account/GenerateHash?password=YourPassword
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult GenerateHash([FromServices] IWebHostEnvironment env, string? password)
        {
            if (!env.IsDevelopment())
                return NotFound();

            if (string.IsNullOrEmpty(password))
                return Content("Usage: /Account/GenerateHash?password=YourPassword", "text/plain");

            var hash = _authService.HashPassword(password);
            return Content($"Password Hash:\n{hash}\n\nUse this hash in your SQL INSERT statement.", "text/plain");
        }
    }
}
