using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using VectorRagDemo.Models.ViewModels;

namespace VectorRagDemo.DAL
{
    public class ConversatieRepository
    {
        private readonly VectorDbContext _context;
        private readonly string _connStr;

        public ConversatieRepository(VectorDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connStr = configuration.GetConnectionString("DefaultConnection")!;
        }

        /// <summary>
        /// Returns a paginated list of conversation rows for a given project, plus the total count.
        /// </summary>
        public async Task<(List<ConversatieRijViewModel> rows, int total)> GetPaginatedAsync(
            int pid, ConversatieFilterModel filter)
        {
            var whereClause = new StringBuilder("WHERE c.projectid = @pid AND c.status = 1");
            var havingClause = new StringBuilder();

            // Kanaal filter
            if (filter.Kanaal != "alle")
                whereClause.Append(" AND c.brontype = @kanaal");

            // Date filters
            if (filter.DatumVan.HasValue)
                whereClause.Append(" AND c.gemaaktop >= @van");

            if (filter.DatumTot.HasValue)
                whereClause.Append(" AND c.gemaaktop < @tot");

            // Zoekterm filter (full-text search in messages)
            if (!string.IsNullOrWhiteSpace(filter.Zoekterm))
                whereClause.Append(" AND EXISTS (SELECT 1 FROM message WHERE conversation = c.id AND tekst ILIKE @zoek)");

            // Type filter: escalatie goes into WHERE, verlaten/betrokken into HAVING
            if (filter.Type == "escalatie")
                whereClause.Append(" AND EXISTS (SELECT 1 FROM escalatieevent WHERE conversatieid = c.id)");
            else if (filter.Type == "verlaten")
                havingClause.Append("HAVING COUNT(m.id) FILTER (WHERE m.sendertype = 1) = 1");
            else if (filter.Type == "betrokken")
                havingClause.Append("HAVING COUNT(m.id) >= 5");

            var orderClause = filter.Sortering == "nieuwste"
                ? "ORDER BY c.gemaaktop DESC"
                : "ORDER BY c.gemaaktop ASC";

            var offset = (filter.Pagina - 1) * ConversatieFilterModel.PaginaGrootte;

            var mainSql = $@"
                SELECT
                    c.id,
                    c.gemaaktop,
                    c.brontype,
                    COUNT(m.id)::int                                                         AS totaal_berichten,
                    COUNT(m.id) FILTER (WHERE m.sendertype = 1)::int                        AS gebruiker_berichten,
                    ROUND(COALESCE(EXTRACT(EPOCH FROM (c.gewijzigdop - c.gemaaktop)), 0) / 60.0, 1)::float AS duur_minuten,
                    (SELECT LEFT(tekst, 120) FROM message WHERE conversation = c.id AND sendertype = 1
                     ORDER BY gemaaktop ASC LIMIT 1)                                        AS eerste_bericht,
                    EXISTS(SELECT 1 FROM escalatieevent WHERE conversatieid = c.id)         AS heeft_escalatie,
                    (SELECT STRING_AGG(DISTINCT kanaal, ',') FROM escalatieevent WHERE conversatieid = c.id) AS kanalen
                FROM conversation c
                LEFT JOIN message m ON m.conversation = c.id
                {whereClause}
                GROUP BY c.id, c.gemaaktop, c.gewijzigdop, c.brontype
                {havingClause}
                {orderClause}
                LIMIT @limit OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM (
                    SELECT c.id
                    FROM conversation c
                    LEFT JOIN message m ON m.conversation = c.id
                    {whereClause}
                    GROUP BY c.id
                    {havingClause}
                ) sub";

            var rows = new List<ConversatieRijViewModel>();
            int total = 0;

            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            // Execute count query
            using (var countCmd = new NpgsqlCommand(countSql, conn))
            {
                AddFilterParameters(countCmd, pid, filter);
                var r = await countCmd.ExecuteScalarAsync();
                total = r is not null and not DBNull ? Convert.ToInt32(r) : 0;
            }

            // Execute main query
            using (var cmd = new NpgsqlCommand(mainSql, conn))
            {
                AddFilterParameters(cmd, pid, filter);
                cmd.Parameters.AddWithValue("limit", ConversatieFilterModel.PaginaGrootte);
                cmd.Parameters.AddWithValue("offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new ConversatieRijViewModel
                    {
                        ID                  = reader.GetInt32(0),
                        GemaaktOp           = reader.GetDateTime(1),
                        BronType            = reader.GetString(2),
                        TotaalBerichten     = reader.GetInt32(3),
                        GebruikerBerichten  = reader.GetInt32(4),
                        DuurMinuten         = reader.GetDouble(5),
                        EersteBerichtPreview = reader.IsDBNull(6) ? null : reader.GetString(6),
                        HeeftEscalatie      = reader.GetBoolean(7),
                        EscalatieKanalen    = reader.IsDBNull(8) ? null : reader.GetString(8)
                    });
                }
            }

            return (rows, total);
        }

        /// <summary>
        /// Returns full detail for a single conversation, verifying it belongs to the given project.
        /// Returns null if not found or does not belong to the project.
        /// </summary>
        public async Task<ConversatieDetailViewModel?> GetDetailAsync(int conversatieId, int pid)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ID == conversatieId && c.ProjectID == pid);

            if (conversation == null) return null;

            var project = await _context.Projects.FindAsync(pid);
            if (project == null) return null;

            var messages = await _context.Messages
                .Where(m => m.Conversation == conversatieId)
                .OrderBy(m => m.GemaaktOp)
                .ToListAsync();

            var escalaties = await _context.EscalatieEvents
                .Where(e => e.ConversatieID == conversatieId)
                .OrderBy(e => e.GemaaktOp)
                .ToListAsync();

            var duurMinuten = conversation.GewijzigdOp > conversation.GemaaktOp
                ? Math.Round((conversation.GewijzigdOp - conversation.GemaaktOp).TotalMinutes, 1)
                : 0;

            return new ConversatieDetailViewModel
            {
                ID           = conversation.ID,
                ProjectID    = pid,
                GemaaktOp    = conversation.GemaaktOp,
                BronType     = conversation.BronType,
                DuurMinuten  = duurMinuten,
                Project      = project,
                Berichten    = messages.Select(m => new BerichtViewModel
                {
                    Tekst     = m.Tekst,
                    GemaaktOp = m.GemaaktOp,
                    IsBot     = m.SenderType == 2
                }).ToList(),
                Escalaties   = escalaties.Select(e => new EscalatieEventRij
                {
                    GemaaktOp = e.GemaaktOp,
                    Kanaal    = e.Kanaal
                }).ToList()
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static void AddFilterParameters(NpgsqlCommand cmd, int pid, ConversatieFilterModel filter)
        {
            cmd.Parameters.AddWithValue("pid", pid);

            if (filter.Kanaal != "alle")
                cmd.Parameters.AddWithValue("kanaal", filter.Kanaal);

            if (filter.DatumVan.HasValue)
                cmd.Parameters.AddWithValue("van", filter.DatumVan.Value);

            if (filter.DatumTot.HasValue)
                cmd.Parameters.AddWithValue("tot", filter.DatumTot.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(filter.Zoekterm))
                cmd.Parameters.AddWithValue("zoek", "%" + filter.Zoekterm.Trim() + "%");
        }
    }
}
