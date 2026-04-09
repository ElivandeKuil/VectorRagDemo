using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.Enums;
using VectorRagDemo.Models.JsonContracts.GeminiAPI;

namespace VectorRagDemo.Services
{
    public class CorrectieService : ServiceBase
    {
        public CorrectieService(VectorDbContext context, LogboekDbContext logboekContext, IConfiguration configuration)
            : base(context, logboekContext, configuration)
        {
        }

        /// <summary>
        /// Processes a factual correction on a bot message.
        /// For the first bot response the original user question is used directly.
        /// For any subsequent response the full conversation up to that message is
        /// contextualised into a single standalone question via the
        /// ContextualizeConversation prompt.
        /// The resulting Q&A pair is embedded and stored as a new chunk (correctie = true)
        /// and a correctie record is saved for tracking.
        /// </summary>
        public async Task SlaCorrectieOpAsync(
            int conversationId,
            int messageId,
            string correctieTekst,
            int projectId)
        {
            var messages = await Context.Messages
                .Where(m => m.Conversation == conversationId)
                .OrderBy(m => m.GemaaktOp)
                .ToListAsync();

            var correctedIndex = messages.FindIndex(m => m.ID == messageId);
            if (correctedIndex < 0)
                throw new InvalidOperationException($"Message {messageId} not found in conversation {conversationId}.");

            var relevantMessages = messages.Take(correctedIndex + 1).ToList();
            var originalTekst    = messages[correctedIndex].Tekst;

            var firstBotIndex    = relevantMessages.FindIndex(m => m.SenderType == 2);
            bool isFirstBotMessage = firstBotIndex == correctedIndex;

            string question;
            if (isFirstBotMessage)
            {
                var firstUserMessage = relevantMessages.FirstOrDefault(m => m.SenderType == 1);
                if (firstUserMessage == null)
                    throw new InvalidOperationException("No user message found before the first bot response.");
                question = firstUserMessage.Tekst;
            }
            else
            {
                var history = BuildConversationHistory(relevantMessages.Take(correctedIndex).ToList());
                var modelService = new GenerativeModelService(Context, HttpClient, LogboekContext, projectId);
                question = await modelService.ExecutePipelineStep<GeminiContextualizeInnerResponse, string>(
                    PromptTypeEnum.ContextualizeConversation,
                    r => r.RewrittenQuestion,
                    history);
            }

            var chunkTekst = FormatChunkTekst(question, correctieTekst.Trim());
            var chunkId    = await InsertChunkAsync(chunkTekst, projectId);

            await InsertCorrectieAsync(
                projectId, conversationId, messageId,
                originalTekst, question, correctieTekst.Trim(), chunkId);
        }

        /// <summary>
        /// Updates an existing correction: regenerates the chunk embedding and
        /// updates both the chunk and the correctie record.
        /// </summary>
        public async Task UpdateCorrectieAsync(int correctieId, string newCorrectietekst, int projectId)
        {
            var correctie = await Context.Correcties
                .FirstOrDefaultAsync(c => c.ID == correctieId && c.ProjectID == projectId && c.Status == 1);

            if (correctie == null)
                throw new InvalidOperationException($"Correctie {correctieId} not found for project {projectId}.");

            var newCorrectietekstTrimmed = newCorrectietekst.Trim();
            var chunkTekst = FormatChunkTekst(correctie.GecontextualiseerdeVraag, newCorrectietekstTrimmed);

            var embedding = await EmbeddingProcessor.GenerateDocumentEmbeddingAsync(chunkTekst);
            if (embedding == null || embedding.Count == 0)
                throw new InvalidOperationException("Failed to generate embedding for updated correction.");

            var vectorString = ToVectorString(embedding);
            var connStr = Configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            const string updateChunkSql = @"
                UPDATE chunk
                SET tekst = @Tekst, tekstvector_mistral = @TekstVector::vector, gewijzigdop = NOW()
                WHERE id = @ChunkID;";

            using var cmd = new NpgsqlCommand(updateChunkSql, conn);
            cmd.Parameters.AddWithValue("@Tekst",       chunkTekst);
            cmd.Parameters.AddWithValue("@TekstVector", vectorString);
            cmd.Parameters.AddWithValue("@ChunkID",     correctie.ChunkID);
            await cmd.ExecuteNonQueryAsync();

            correctie.Correctietekst = newCorrectietekstTrimmed;
            correctie.GewijzigdOp    = DateTime.Now;
            await Context.SaveChangesAsync();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static string FormatChunkTekst(string question, string answer)
            => $"Vraag: {question}\n\nAntwoord: {answer}";

        private static string ToVectorString(List<float> embedding)
            => "[" + string.Join(",", embedding.Select(f =>
                f.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

        private async Task<int> InsertChunkAsync(string chunkTekst, int projectId)
        {
            var embedding = await EmbeddingProcessor.GenerateDocumentEmbeddingAsync(chunkTekst);
            if (embedding == null || embedding.Count == 0)
                throw new InvalidOperationException("Failed to generate embedding for correction chunk.");

            var vectorString = ToVectorString(embedding);
            var bronId       = await GetOrCreateCorrectionsBronAsync(projectId);
            var connStr      = Configuration.GetConnectionString("DefaultConnection")!;

            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO chunk (bronid, tekst, tekstvector_mistral, gemaaktop, status, correctie)
                VALUES (@BronID, @Tekst, @TekstVector::vector, NOW(), 1, true)
                RETURNING id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@BronID",      bronId);
            cmd.Parameters.AddWithValue("@Tekst",       chunkTekst);
            cmd.Parameters.AddWithValue("@TekstVector", vectorString);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task InsertCorrectieAsync(
            int projectId, int conversatieId, int messageId,
            string originalTekst, string question, string correctieTekst, int chunkId)
        {
            var record = new Correctie
            {
                ProjectID                = projectId,
                ConversatieID            = conversatieId,
                MessageID                = messageId,
                OrigineleTekst           = originalTekst,
                GecontextualiseerdeVraag = question,
                Correctietekst           = correctieTekst,
                ChunkID                  = chunkId,
                GemaaktOp                = DateTime.Now,
                Status                   = 1
            };

            Context.Correcties.Add(record);
            await Context.SaveChangesAsync();
        }

        private static string BuildConversationHistory(
            IList<VectorRagDemo.Models.Entities.Management.Message> messages)
        {
            var sb = new StringBuilder();
            foreach (var msg in messages)
            {
                var role = msg.SenderType == 1 ? "Bezoeker" : "Assistent";
                sb.AppendLine($"{role}: {msg.Tekst}");
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<int> GetOrCreateCorrectionsBronAsync(int projectId)
        {
            const string bronTitle = "Correcties";

            var existing = await Context.Bronnen
                .Where(b => b.Project == projectId && b.Title == bronTitle && b.Status == 1)
                .Select(b => b.ID)
                .FirstOrDefaultAsync();

            if (existing != 0)
                return existing;

            var bron = new Bron
            {
                Title     = bronTitle,
                Project   = projectId,
                GemaaktOp = DateTime.Now,
                Status    = 1
            };

            Context.Bronnen.Add(bron);
            await Context.SaveChangesAsync();
            return bron.ID;
        }
    }
}
