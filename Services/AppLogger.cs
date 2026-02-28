using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;

namespace VectorRagDemo.Services
{
    public static class AppLogger
    {
        private static IServiceScopeFactory? _scopeFactory;

        public static void Initialize(IServiceScopeFactory factory) => _scopeFactory = factory;

        public static void Log(string message, string source = "", string? detail = null)
            => WriteAsync("Info", source, message, detail);

        public static void LogWarning(string message, string source = "", string? detail = null)
            => WriteAsync("Warning", source, message, detail);

        public static void LogError(string message, string source = "", string? detail = null)
            => WriteAsync("Error", source, message, detail);

        private static void WriteAsync(string level, string source, string message, string? detail)
        {
            if (_scopeFactory == null) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<LogboekDbContext>();
                    db.AppLogs.Add(new AppLog
                    {
                        Level = level,
                        Source = source,
                        Message = message,
                        Detail = detail,
                        GemaaktOp = DateTime.Now
                    });
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Swallow — the logger must never crash the application
                }
            });
        }
    }
}
