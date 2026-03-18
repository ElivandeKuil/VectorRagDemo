using Microsoft.EntityFrameworkCore;
using Npgsql;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.DAL
{
    public class StatistiekenRepository
    {
        private readonly VectorDbContext _context;
        private readonly string _connStr;

        private readonly string _logboekConnStr;

        public StatistiekenRepository(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connStr = configuration.GetConnectionString("DefaultConnection")!;
            _logboekConnStr = configuration.GetConnectionString("LogboekConnection")!;
        }

        // ── Tier 1 ────────────────────────────────────────────────────────────

        public Task<int> GetTotaalGesprekkenAsync(int pid) =>
            _context.Conversations.Where(c => c.ProjectID == pid && c.Status == 1).CountAsync();

        public Task<int> GetGesprekkenVanafAsync(int pid, DateTime van) =>
            _context.Conversations.Where(c => c.ProjectID == pid && c.Status == 1 && c.GemaaktOp >= van).CountAsync();

        public Task<int> GetGesprekkenTussenAsync(int pid, DateTime van, DateTime tot) =>
            _context.Conversations.Where(c => c.ProjectID == pid && c.Status == 1 && c.GemaaktOp >= van && c.GemaaktOp < tot).CountAsync();

        public async Task<int> GetTotaalBerichtenAsync(int pid)
        {
            var ids = _context.Conversations.Where(c => c.ProjectID == pid && c.Status == 1).Select(c => c.ID);
            return await _context.Messages.Where(m => ids.Contains(m.Conversation)).CountAsync();
        }

        public async Task<Dictionary<DateTime, int>> GetGesprekkenPerDagAsync(int pid, DateTime van)
        {
            var result = new Dictionary<DateTime, int>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT DATE(gemaaktop) AS dag, COUNT(*)::int AS aantal
                FROM   conversation
                WHERE  projectid = @pid AND status = 1 AND gemaaktop >= @van
                GROUP  BY dag
                ORDER  BY dag", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result[reader.GetDateTime(0)] = reader.GetInt32(1);
            return result;
        }

        // ── Tier 2 ────────────────────────────────────────────────────────────

        public async Task<double> GetGemiddeldBerichtenPerGesprekAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT COALESCE(AVG(msg_count), 0)
                FROM (
                    SELECT COUNT(*) AS msg_count
                    FROM   message m
                    INNER JOIN conversation c ON m.conversation = c.id
                    WHERE  c.projectid = @pid AND c.status = 1
                    GROUP  BY m.conversation
                ) sub", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Math.Round(Convert.ToDouble(r), 1) : 0;
        }

        public async Task<double> GetGemiddeldeDuurMinutenAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT COALESCE(AVG(EXTRACT(EPOCH FROM (gewijzigdop - gemaaktop)) / 60.0), 0)
                FROM   conversation
                WHERE  projectid = @pid AND status = 1 AND gewijzigdop > gemaaktop", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Math.Round(Convert.ToDouble(r), 1) : 0;
        }

        public async Task<int[]> GetGesprekkenPerUurAsync(int pid, DateTime van)
        {
            var result = new int[24];
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT EXTRACT(HOUR FROM gemaaktop)::int AS uur, COUNT(*)::int AS aantal
                FROM   conversation
                WHERE  projectid = @pid AND status = 1 AND gemaaktop >= @van
                GROUP  BY uur
                ORDER  BY uur", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetInt32(0)] = r.GetInt32(1);
            return result;
        }

        public async Task<int[]> GetGesprekkenPerDagVanDeWeekAsync(int pid, DateTime van)
        {
            var result = new int[7];
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT (EXTRACT(ISODOW FROM gemaaktop)::int - 1) AS dag, COUNT(*)::int AS aantal
                FROM   conversation
                WHERE  projectid = @pid AND status = 1 AND gemaaktop >= @van
                GROUP  BY dag
                ORDER  BY dag", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetInt32(0)] = r.GetInt32(1);
            return result;
        }

        public async Task<(int gebruiker, int bot)> GetBerichtenPerSenderTypeAsync(int pid)
        {
            int gebruiker = 0, bot = 0;
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT m.sendertype, COUNT(*)::int AS aantal
                FROM   message m
                INNER JOIN conversation c ON m.conversation = c.id
                WHERE  c.projectid = @pid AND c.status = 1
                GROUP  BY m.sendertype", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                if (r.GetInt32(0) == 1) gebruiker = r.GetInt32(1);
                else if (r.GetInt32(0) == 2) bot = r.GetInt32(1);
            }
            return (gebruiker, bot);
        }

        public async Task<Dictionary<DateTime, int>> GetGesprekkenPerMaandAsync(int pid, DateTime van)
        {
            var result = new Dictionary<DateTime, int>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT DATE_TRUNC('month', gemaaktop) AS maand, COUNT(*)::int AS aantal
                FROM   conversation
                WHERE  projectid = @pid AND status = 1 AND gemaaktop >= @van
                GROUP  BY maand
                ORDER  BY maand", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetDateTime(0)] = r.GetInt32(1);
            return result;
        }

        public async Task<Dictionary<DateTime, int>> GetBerichtenPerDagAsync(int pid, DateTime van)
        {
            var result = new Dictionary<DateTime, int>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT DATE(m.gemaaktop) AS dag, COUNT(*)::int AS aantal
                FROM   message m
                INNER JOIN conversation c ON m.conversation = c.id
                WHERE  c.projectid = @pid AND c.status = 1 AND m.gemaaktop >= @van
                GROUP  BY dag
                ORDER  BY dag", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetDateTime(0)] = r.GetInt32(1);
            return result;
        }

        public async Task<int> GetBetrokkenGesprekkenAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*)::int
                FROM (
                    SELECT c.id
                    FROM   conversation c
                    INNER JOIN message m ON m.conversation = c.id
                    WHERE  c.projectid = @pid AND c.status = 1
                    GROUP  BY c.id
                    HAVING COUNT(m.id) >= 5
                ) sub", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Convert.ToInt32(r) : 0;
        }

        // ── Effectiviteit ─────────────────────────────────────────────────────

        /// <summary>Conversations where the user sent exactly 1 message — a likely abandonment signal.</summary>
        public async Task<int> GetVerlatenGesprekkenAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*)::int
                FROM (
                    SELECT c.id
                    FROM   message m
                    INNER JOIN conversation c ON m.conversation = c.id
                    WHERE  c.projectid = @pid AND c.status = 1 AND m.sendertype = 1
                    GROUP  BY c.id
                    HAVING COUNT(m.id) = 1
                ) sub", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Convert.ToInt32(r) : 0;
        }

        public Task<int> GetUniekeBezoekersAsync(int pid, DateTime van) =>
            _context.Conversations
                .Where(c => c.ProjectID == pid && c.Status == 1
                         && c.BronType == "widget" && c.SessionToken != null
                         && c.GemaaktOp >= van)
                .Select(c => c.SessionToken)
                .Distinct()
                .CountAsync();

        /// <summary>Escalations per month for the last 6 months.</summary>
        public async Task<Dictionary<DateTime, int>> GetEscalatiesPerMaandAsync(int pid, DateTime van)
        {
            var result = new Dictionary<DateTime, int>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT DATE_TRUNC('month', gemaaktop) AS maand, COUNT(*)::int AS aantal
                FROM   escalatieevent
                WHERE  projectid = @pid AND gemaaktop >= @van
                GROUP  BY maand
                ORDER  BY maand", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetDateTime(0)] = r.GetInt32(1);
            return result;
        }

        /// <summary>Average message count for escalated conversations vs. non-escalated ones.</summary>
        public async Task<(double geescaleerd, double afgehandeld)> GetGemBerichtenPerUitkomstAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT
                    geescaleerd,
                    ROUND(AVG(msg_count)::numeric, 1) AS gem
                FROM (
                    SELECT
                        c.id,
                        COUNT(m.id) AS msg_count,
                        EXISTS (
                            SELECT 1 FROM escalatieevent ee
                            WHERE  ee.conversatieid = c.id AND ee.projectid = @pid
                        ) AS geescaleerd
                    FROM   conversation c
                    LEFT JOIN message m ON m.conversation = c.id
                    WHERE  c.projectid = @pid AND c.status = 1
                    GROUP  BY c.id
                ) sub
                GROUP  BY geescaleerd", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            double geescaleerd = 0, afgehandeld = 0;
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var gem = r.IsDBNull(1) ? 0 : Convert.ToDouble(r.GetValue(1));
                if (r.GetBoolean(0)) geescaleerd = gem;
                else afgehandeld = gem;
            }
            return (geescaleerd, afgehandeld);
        }

        // ── Prestaties (based on ApiCallLog.DurationMs, grouped by CorrelationId) ─────

        // Groups ApiCallLog rows by CorrelationId and sums DurationMs to get the total
        // perceived response time per user interaction (full prompt pipeline).
        // HAVING COUNT(*) >= 2 ensures only interactions where the shared CorrelationId
        // was propagated across all pipeline calls are included — old log entries where
        // each call had its own random Guid (count=1) are excluded automatically.
        // Stats are system-wide — no ProjectID on ApiCallLog.
        // Only include CorrelationId groups with 2+ calls — these are interactions where
        // the shared CorrelationId was propagated across the full prompt pipeline.
        // Single-call groups are old log entries (each call had its own random Guid).
        private const string ResponstijdSubquery = @"
            SELECT SUM(durationms) AS total_ms
            FROM   apicalllog
            WHERE  correlationid IS NOT NULL
              AND  durationms    IS NOT NULL
            GROUP  BY correlationid
            HAVING COUNT(*) >= 2";

        public async Task<double> GetGemiddeldeResponstijdSecAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_logboekConnStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand($@"
                SELECT COALESCE(AVG(total_ms), 0) / 1000.0
                FROM ({ResponstijdSubquery}) sub", conn);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Math.Round(Convert.ToDouble(r), 1) : 0;
        }

        public async Task<double> GetP95ResponstijdSecAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_logboekConnStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand($@"
                SELECT COALESCE(PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY total_ms), 0) / 1000.0
                FROM ({ResponstijdSubquery}) sub", conn);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Math.Round(Convert.ToDouble(r), 1) : 0;
        }

        public async Task<Dictionary<DateTime, double>> GetResponstijdPerDagAsync(int pid, DateTime van)
        {
            var result = new Dictionary<DateTime, double>();
            using var conn = new NpgsqlConnection(_logboekConnStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT DATE(ts) AS dag, ROUND(AVG(total_ms)::numeric / 1000.0, 1) AS gem_sec
                FROM (
                    SELECT correlationid, SUM(durationms) AS total_ms, MIN(gemaaktop) AS ts
                    FROM   apicalllog
                    WHERE  correlationid IS NOT NULL
                      AND  durationms    IS NOT NULL
                      AND  gemaaktop     >= @van
                    GROUP  BY correlationid
                    HAVING COUNT(*) >= 2
                ) sub
                GROUP  BY dag
                ORDER  BY dag", conn);
            cmd.Parameters.AddWithValue("van", van);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetDateTime(0)] = Convert.ToDouble(r.GetValue(1));
            return result;
        }

        // ── Escalatie ─────────────────────────────────────────────────────────

        public Task<int> GetTotaalEscalatiesAsync(int pid) =>
            _context.EscalatieEvents.Where(e => e.ProjectID == pid).CountAsync();

        public async Task<(int whatsapp, int email)> GetEscalatiesPerKanaalAsync(int pid)
        {
            var byKanaal = await _context.EscalatieEvents
                .Where(e => e.ProjectID == pid)
                .GroupBy(e => e.Kanaal)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            return (
                byKanaal.FirstOrDefault(k => k.Key == "whatsapp")?.Count ?? 0,
                byKanaal.FirstOrDefault(k => k.Key == "email")?.Count    ?? 0
            );
        }

        public Task<int> GetGesprekkenMetEscalatieAsync(int pid) =>
            _context.EscalatieEvents
                .Where(e => e.ProjectID == pid && e.ConversatieID != null)
                .Select(e => e.ConversatieID)
                .Distinct()
                .CountAsync();

        public async Task<double> GetGemiddeldBerichtenVoorEscalatieAsync(int pid)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"
                SELECT COALESCE(AVG(msg_before), 0)
                FROM (
                    SELECT ee.conversatieid,
                           COUNT(m.id)::float AS msg_before
                    FROM   escalatieevent ee
                    INNER JOIN conversation c ON ee.conversatieid = c.id
                    LEFT  JOIN message m ON m.conversation = c.id
                        AND m.gemaaktop < (
                            SELECT MIN(gemaaktop)
                            FROM   escalatieevent
                            WHERE  conversatieid = ee.conversatieid
                        )
                    WHERE  ee.projectid = @pid
                    GROUP  BY ee.conversatieid
                ) sub", conn);
            cmd.Parameters.AddWithValue("pid", pid);
            var r = await cmd.ExecuteScalarAsync();
            return r is not null and not DBNull ? Math.Round(Convert.ToDouble(r), 1) : 0;
        }
    }
}
