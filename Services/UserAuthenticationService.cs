using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Services
{
    public class UserAuthenticationService
    {
        private readonly ManagementDbContext _context;
        private readonly PasswordHasher<Gebruiker> _passwordHasher;

        public UserAuthenticationService(ManagementDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Gebruiker>();
        }

        public async Task<Gebruiker?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Gebruikers
                .FirstOrDefaultAsync(u => u.GebruikersNaam == username && u.Status == 1);

            if (user == null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Wachtwoord, password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // If password needs rehashing, update it
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Wachtwoord = _passwordHasher.HashPassword(user, password);
                user.GewijzigdOp = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return user;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            return await _context.Rollen
                .FromSqlInterpolated(@$"
                    SELECT Rol.*
                        FROM Management.dbo.GebruikerRol
                        INNER JOIN Management.dbo.Rol ON Rol.ID = GebruikerRol.Rol
                        WHERE GebruikerRol.Status = 1 
                        AND Rol.Status = 1
                        AND GebruikerRol.Gebruiker = {userId}
                    ")
                .Select(r => r.Omschrijving)
                .ToListAsync();
        }

        public string HashPassword(string password)
        {
            var user = new Gebruiker();
            return _passwordHasher.HashPassword(user, password);
        }
    }
}
