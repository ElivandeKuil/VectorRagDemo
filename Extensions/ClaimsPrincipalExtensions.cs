using System.Security.Claims;

namespace VectorRagDemo.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User ID claim not found.");
            return int.Parse(value);
        }
    }
}
