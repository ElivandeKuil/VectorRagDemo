using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Services
{
    // Achtergrondservice die periodiek oude logregels verwijdert conform GDPR-principe
    // van opslagbeperking (Art. 5(1)(e)).
    // Standaard retentietermijnen:
    //   AppLogs      : 90 dagen
    //   ApiCallLogs  : 30 dagen
    // Overschrijfbaar via omgevingsvariabelen RETENTION_APPLOG_DAYS en RETENTION_APICALLLOG_DAYS.
    public class LogRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LogRetentionService> _logger;
        private readonly int _appLogRetentionDays;
        private readonly int _apiCallLogRetentionDays;

        public LogRetentionService(IServiceScopeFactory scopeFactory, ILogger<LogRetentionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _appLogRetentionDays = int.TryParse(
                Environment.GetEnvironmentVariable("RETENTION_APPLOG_DAYS"), out var a) ? a : 90;

            _apiCallLogRetentionDays = int.TryParse(
                Environment.GetEnvironmentVariable("RETENTION_APICALLLOG_DAYS"), out var b) ? b : 30;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeOldLogsAsync();
                // Elke 24 uur uitvoeren
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task PurgeOldLogsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LogboekDbContext>();

                var appLogCutoff = DateTime.UtcNow.AddDays(-_appLogRetentionDays);
                var apiCallLogCutoff = DateTime.UtcNow.AddDays(-_apiCallLogRetentionDays);

                var deletedAppLogs = await db.AppLogs
                    .Where(l => l.GemaaktOp < appLogCutoff)
                    .ExecuteDeleteAsync();

                var deletedApiLogs = await db.ApiCallLogs
                    .Where(l => l.GemaaktOp < apiCallLogCutoff)
                    .ExecuteDeleteAsync();

                if (deletedAppLogs > 0 || deletedApiLogs > 0)
                {
                    _logger.LogInformation(
                        "Log-retentie: {AppLogs} applicatielogs (>{AppDays}d) en {ApiLogs} API-logs (>{ApiDays}d) verwijderd.",
                        deletedAppLogs, _appLogRetentionDays,
                        deletedApiLogs, _apiCallLogRetentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij uitvoeren van log-retentieopruiming.");
            }
        }
    }
}
