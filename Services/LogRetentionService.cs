using Microsoft.EntityFrameworkCore;
using VectorRagDemo.DAL;

namespace VectorRagDemo.Services
{
    // Achtergrondservice die periodiek verlopen gegevens verwijdert conform GDPR-principe
    // van opslagbeperking (Art. 5(1)(e)).
    //
    // Standaard retentietermijnen (overschrijfbaar via omgevingsvariabelen):
    //   AppLogs                : 90 dagen  (RETENTION_APPLOG_DAYS)
    //   ApiCallLogs            : 30 dagen  (RETENTION_APICALLLOG_DAYS)
    //   Widget-gesprekken      : 30 dagen  (RETENTION_WIDGET_CONVERSATION_DAYS)
    public class LogRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LogRetentionService> _logger;
        private readonly int _appLogRetentionDays;
        private readonly int _apiCallLogRetentionDays;
        private readonly int _widgetConversationRetentionDays;

        public LogRetentionService(IServiceScopeFactory scopeFactory, ILogger<LogRetentionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _appLogRetentionDays = int.TryParse(
                Environment.GetEnvironmentVariable("RETENTION_APPLOG_DAYS"), out var a) ? a : 90;

            _apiCallLogRetentionDays = int.TryParse(
                Environment.GetEnvironmentVariable("RETENTION_APICALLLOG_DAYS"), out var b) ? b : 30;

            _widgetConversationRetentionDays = int.TryParse(
                Environment.GetEnvironmentVariable("RETENTION_WIDGET_CONVERSATION_DAYS"), out var c) ? c : 30;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeOldLogsAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task PurgeOldLogsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var logboekDb = scope.ServiceProvider.GetRequiredService<LogboekDbContext>();
                var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbContext>();

                var appLogCutoff = DateTime.UtcNow.AddDays(-_appLogRetentionDays);
                var apiCallLogCutoff = DateTime.UtcNow.AddDays(-_apiCallLogRetentionDays);
                var widgetCutoff = DateTime.UtcNow.AddDays(-_widgetConversationRetentionDays);

                var deletedAppLogs = await logboekDb.AppLogs
                    .Where(l => l.GemaaktOp < appLogCutoff)
                    .ExecuteDeleteAsync();

                var deletedApiLogs = await logboekDb.ApiCallLogs
                    .Where(l => l.GemaaktOp < apiCallLogCutoff)
                    .ExecuteDeleteAsync();

                // Widget-gesprekken: verwijder eerst berichten, dan gesprekken
                var expiredWidgetConversationIds = await vectorDb.Conversations
                    .Where(c => c.BronType == "widget" && c.GemaaktOp < widgetCutoff)
                    .Select(c => c.ID)
                    .ToListAsync();

                var deletedWidgetMessages = 0;
                var deletedWidgetConversations = 0;

                if (expiredWidgetConversationIds.Count > 0)
                {
                    deletedWidgetMessages = await vectorDb.Messages
                        .Where(m => expiredWidgetConversationIds.Contains(m.Conversation))
                        .ExecuteDeleteAsync();

                    deletedWidgetConversations = await vectorDb.Conversations
                        .Where(c => expiredWidgetConversationIds.Contains(c.ID))
                        .ExecuteDeleteAsync();
                }

                if (deletedAppLogs > 0 || deletedApiLogs > 0 || deletedWidgetConversations > 0)
                {
                    _logger.LogInformation(
                        "Retentie: {AppLogs} applogs (>{AppDays}d), {ApiLogs} API-logs (>{ApiDays}d), " +
                        "{WConv} widget-gesprekken + {WMsg} berichten (>{WDays}d) verwijderd.",
                        deletedAppLogs, _appLogRetentionDays,
                        deletedApiLogs, _apiCallLogRetentionDays,
                        deletedWidgetConversations, deletedWidgetMessages, _widgetConversationRetentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij uitvoeren van retentieopruiming.");
            }
        }
    }
}
