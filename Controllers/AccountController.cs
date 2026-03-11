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
        private readonly ConsentService _consentService;
        private readonly DataExportService _exportService;

        public AccountController(
            UserAuthenticationService authService,
            ProjectAccessService projectAccessService,
            ConsentService consentService,
            DataExportService exportService)
        {
            _authService = authService;
            _projectAccessService = projectAccessService;
            _consentService = consentService;
            _exportService = exportService;
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

            // GDPR: controleer of de gebruiker toestemming heeft gegeven voor de huidige beleidsversie
            var hasConsent = await _consentService.HasActiveConsentAsync(user.ID);
            if (!hasConsent)
                claims.Add(new Claim("ConsentRequired", "true"));

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

            if (!hasConsent)
                return RedirectToAction("Toestemming", "Account", new { returnUrl });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        // ── Toestemming ──────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Toestemming(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Gebruiker die al toestemming heeft kan de pagina bezoeken om geschiedenis te zien
            var userId = User.GetUserId();
            ViewBag.ConsentHistory = await _consentService.GetConsentHistoryAsync(userId);
            ViewBag.HasActiveConsent = await _consentService.HasActiveConsentAsync(userId);
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToestemmingAkkoord(string? returnUrl = null)
        {
            var userId = User.GetUserId();
            var ip = AnonymizeIp(HttpContext.Connection.RemoteIpAddress?.ToString());
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 500) userAgent = userAgent[..500];

            await _consentService.RecordConsentAsync(userId, ip, userAgent);

            // Vernieuwen van cookie zonder ConsentRequired-claim
            await RefreshAuthCookieAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToestemmingWeigeren()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToestemmingIntrekken()
        {
            var userId = User.GetUserId();
            await _consentService.RevokeConsentAsync(userId);

            // Uitloggen na intrekking: sessie bevat geen geldige toestemming meer
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Uw toestemming is ingetrokken. U bent uitgelogd.";
            return RedirectToAction("Login", "Account");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        // Vernieuwt het auth-cookie na het geven van toestemming door de claims opnieuw
        // op te bouwen zonder de ConsentRequired-claim.
        private async Task RefreshAuthCookieAsync()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null) return;

            var filteredClaims = result.Principal.Claims
                .Where(c => c.Type != "ConsentRequired")
                .ToList();

            var identity = new ClaimsIdentity(filteredClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var props = result.Properties;

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                props);
        }

        private static string? AnonymizeIp(string? ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;
            var parts = ip.Split('.');
            if (parts.Length == 4) return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
            var groups = ip.Split(':');
            if (groups.Length >= 3) return string.Join(":", groups.Take(3)) + "::/48";
            return null;
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

        // Publiek toegankelijk — geen login vereist (Art. 13/14 GDPR)
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Privacyverklaring() => View();

        // DSAR: gebruiker downloadt zijn eigen gegevens (Art. 15 GDPR)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MijnGegevens()
        {
            var userId = User.GetUserId();
            var export = await _exportService.BuildExportAsync(userId);
            var json = System.Text.Json.JsonSerializer.Serialize(export,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var filename = $"mijn-gegevens-{DateTime.UtcNow:yyyy-MM-dd}.json";
            return File(bytes, "application/json", filename);
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
