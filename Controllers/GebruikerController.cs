using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GebruikerController : Controller
    {
        private readonly ManagementDbContext _context;
        private readonly UserAuthenticationService _authService;

        public GebruikerController(ManagementDbContext context, UserAuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: /Gebruiker
        public async Task<IActionResult> Index()
        {
            var gebruikers = await _context.Gebruikers
                .OrderBy(g => g.Naam)
                .ToListAsync();

            var viewModels = new List<GebruikerListItemViewModel>();

            foreach (var gebruiker in gebruikers)
            {
                var rollen = await _context.GebruikerRollen
                    .Where(gr => gr.Gebruiker == gebruiker.ID && gr.Status == 1)
                    .Include(gr => gr.RolNavigation)
                    .Select(gr => gr.RolNavigation.Omschrijving)
                    .ToListAsync();

                viewModels.Add(new GebruikerListItemViewModel
                {
                    Gebruiker = gebruiker,
                    Rollen = rollen
                });
            }

            return View(viewModels);
        }

        // GET: /Gebruiker/Create
        public async Task<IActionResult> Create()
        {
            var vm = new GebruikerViewModel();
            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        // POST: /Gebruiker/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GebruikerViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Wachtwoord))
                ModelState.AddModelError(nameof(vm.Wachtwoord), "Wachtwoord is verplicht bij aanmaken.");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            if (await _context.Gebruikers.AnyAsync(g => g.GebruikersNaam == vm.GebruikersNaam))
            {
                ModelState.AddModelError(nameof(vm.GebruikersNaam), "Deze gebruikersnaam is al in gebruik.");
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            var gebruiker = new Gebruiker
            {
                Naam = vm.Naam.Trim(),
                GebruikersNaam = vm.GebruikersNaam.Trim(),
                Wachtwoord = _authService.HashPassword(vm.Wachtwoord!),
                GewijzigdOp = DateTime.Now,
                GemaaktOp = DateTime.Now,
                Status = 1,
                WachtwoordWijzigen = true
            };

            _context.Gebruikers.Add(gebruiker);
            await _context.SaveChangesAsync();

            await SaveRollenAsync(gebruiker.ID, vm.GeselecteerdeRollen);
            await SaveProjectenAsync(gebruiker.ID, vm.GeselecteerdeProjecten);
            await SaveSubProjectenAsync(gebruiker.ID, vm.GeselecteerdeSubProjecten);

            TempData["Success"] = $"Gebruiker '{gebruiker.Naam}' is aangemaakt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Gebruiker/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var gebruiker = await _context.Gebruikers.FindAsync(id);
            if (gebruiker == null) return NotFound();

            var geselecteerdeRollen = await _context.GebruikerRollen
                .Where(gr => gr.Gebruiker == id && gr.Status == 1)
                .Select(gr => gr.Rol)
                .ToListAsync();

            var geselecteerdeProjecten = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == id && gp.Status == 1)
                .Select(gp => gp.Project)
                .ToListAsync();

            var geselecteerdeSubProjecten = await _context.GebruikerSubProjecten
                .Where(gs => gs.Gebruiker == id && gs.Status == 1)
                .Select(gs => gs.SubProject)
                .ToListAsync();

            var vm = new GebruikerViewModel
            {
                ID = gebruiker.ID,
                Naam = gebruiker.Naam,
                GebruikersNaam = gebruiker.GebruikersNaam,
                GeselecteerdeRollen = geselecteerdeRollen,
                GeselecteerdeProjecten = geselecteerdeProjecten,
                GeselecteerdeSubProjecten = geselecteerdeSubProjecten
            };

            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        // POST: /Gebruiker/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GebruikerViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            var gebruiker = await _context.Gebruikers.FindAsync(id);
            if (gebruiker == null) return NotFound();

            if (await _context.Gebruikers.AnyAsync(g => g.GebruikersNaam == vm.GebruikersNaam && g.ID != id))
            {
                ModelState.AddModelError(nameof(vm.GebruikersNaam), "Deze gebruikersnaam is al in gebruik.");
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            gebruiker.Naam = vm.Naam.Trim();
            gebruiker.GebruikersNaam = vm.GebruikersNaam.Trim();
            gebruiker.GewijzigdOp = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(vm.Wachtwoord))
                gebruiker.Wachtwoord = _authService.HashPassword(vm.Wachtwoord);

            await _context.SaveChangesAsync();

            await SaveRollenAsync(id, vm.GeselecteerdeRollen);
            await SaveProjectenAsync(id, vm.GeselecteerdeProjecten);
            await SaveSubProjectenAsync(id, vm.GeselecteerdeSubProjecten);

            TempData["Success"] = $"Gebruiker '{gebruiker.Naam}' is bijgewerkt.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Gebruiker/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var gebruiker = await _context.Gebruikers.FindAsync(id);
            if (gebruiker == null) return NotFound();

            gebruiker.Status = gebruiker.Status == 1 ? 0 : 1;
            gebruiker.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Gebruiker '{gebruiker.Naam}' is {(gebruiker.Status == 1 ? "geactiveerd" : "gedeactiveerd")}.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Private helpers ----

        private async Task LoadDropdownsAsync(GebruikerViewModel vm)
        {
            vm.BeschikbareRollen = await _context.Rollen
                .Where(r => r.Status == 1)
                .OrderBy(r => r.Omschrijving)
                .ToListAsync();

            vm.BeschikbareProjecten = await _context.MgmtProjects
                .Where(p => p.Status == 1)
                .OrderBy(p => p.Omschrijving)
                .ToListAsync();

            vm.BeschikbareSubProjecten = await _context.MgmtSubProjects
                .Where(sp => sp.Status == 1)
                .Include(sp => sp.ProjectNavigation)
                .OrderBy(sp => sp.ProjectNavigation.Omschrijving)
                .ThenBy(sp => sp.Omschrijving)
                .ToListAsync();
        }

        private async Task SaveRollenAsync(int gebruikerId, List<int> geselecteerdeRolIds)
        {
            var bestaande = await _context.GebruikerRollen
                .Where(gr => gr.Gebruiker == gebruikerId)
                .ToListAsync();

            foreach (var bestaand in bestaande)
            {
                if (geselecteerdeRolIds.Contains(bestaand.Rol))
                {
                    // Re-activate if deactivated
                    if (bestaand.Status != 1)
                    {
                        bestaand.Status = 1;
                        bestaand.GewijzigdOp = DateTime.Now;
                    }
                }
                else
                {
                    bestaand.Status = 0;
                    bestaand.GewijzigdOp = DateTime.Now;
                }
            }

            var bestaandeRolIds = bestaande.Select(gr => gr.Rol).ToHashSet();
            foreach (var rolId in geselecteerdeRolIds.Where(r => !bestaandeRolIds.Contains(r)))
            {
                _context.GebruikerRollen.Add(new GebruikerRol
                {
                    Gebruiker = gebruikerId,
                    Rol = rolId,
                    GemaaktOp = DateTime.Now,
                    Status = 1
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SaveProjectenAsync(int gebruikerId, List<int> geselecteerdeProjectIds)
        {
            var bestaande = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == gebruikerId)
                .ToListAsync();

            foreach (var bestaand in bestaande)
            {
                if (geselecteerdeProjectIds.Contains(bestaand.Project))
                {
                    if (bestaand.Status != 1)
                    {
                        bestaand.Status = 1;
                        bestaand.GewijzigdOp = DateTime.Now;
                    }
                }
                else
                {
                    bestaand.Status = 0;
                    bestaand.GewijzigdOp = DateTime.Now;
                }
            }

            var bestaandeProjectIds = bestaande.Select(gp => gp.Project).ToHashSet();
            foreach (var projectId in geselecteerdeProjectIds.Where(p => !bestaandeProjectIds.Contains(p)))
            {
                _context.GebruikerProjecten.Add(new Models.Entities.Management.GebruikerProject
                {
                    Gebruiker = gebruikerId,
                    Project = projectId,
                    GemaaktOp = DateTime.Now,
                    GewijzigdOp = DateTime.Now,
                    Status = 1
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SaveSubProjectenAsync(int gebruikerId, List<int> geselecteerdeSubProjectIds)
        {
            var bestaande = await _context.GebruikerSubProjecten
                .Where(gs => gs.Gebruiker == gebruikerId)
                .ToListAsync();

            foreach (var bestaand in bestaande)
            {
                if (geselecteerdeSubProjectIds.Contains(bestaand.SubProject))
                {
                    if (bestaand.Status != 1)
                    {
                        bestaand.Status = 1;
                        bestaand.GewijzigdOp = DateTime.Now;
                    }
                }
                else
                {
                    bestaand.Status = 0;
                    bestaand.GewijzigdOp = DateTime.Now;
                }
            }

            var bestaandeSubProjectIds = bestaande.Select(gs => gs.SubProject).ToHashSet();
            foreach (var subProjectId in geselecteerdeSubProjectIds.Where(sp => !bestaandeSubProjectIds.Contains(sp)))
            {
                _context.GebruikerSubProjecten.Add(new Models.Entities.Management.GebruikerSubProject
                {
                    Gebruiker = gebruikerId,
                    SubProject = subProjectId,
                    GemaaktOp = DateTime.Now,
                    GewijzigdOp = DateTime.Now,
                    Status = 1
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
