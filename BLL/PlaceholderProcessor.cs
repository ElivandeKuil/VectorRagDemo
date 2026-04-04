using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.BLL
{
    public class PlaceholderProcessor
    {
        private static readonly Regex PlaceholderPattern = new(@"@@(\w+)", RegexOptions.Compiled);

        private readonly VectorDbContext _context;

        public PlaceholderProcessor(VectorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Loads all active placeholders for a project as a case-insensitive name→value map.
        /// </summary>
        public async Task<Dictionary<string, string>> GetPlaceholdersAsync(int projectId)
        {
            var list = await _context.Placeholders
                .Where(p => p.ProjectID == projectId && p.Status == 1)
                .Select(p => new { p.Naam, p.Waarde })
                .ToListAsync();

            // Case-insensitive lookup
            return list.ToDictionary(
                p => p.Naam,
                p => p.Waarde,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Replaces all @@naam occurrences in text with their stored value.
        /// Unknown placeholders are left as-is.
        /// </summary>
        public static string Resolve(string text, Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(text) || placeholders.Count == 0)
                return text;

            return PlaceholderPattern.Replace(text, match =>
            {
                var name = match.Groups[1].Value;
                return placeholders.TryGetValue(name, out var value) ? value : match.Value;
            });
        }
    }
}
