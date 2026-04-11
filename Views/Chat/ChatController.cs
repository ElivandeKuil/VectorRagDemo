using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Entities;
using VectorRagDemo.Models.Entities.Management;
using VectorRagDemo.Models.Requests;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Views.Chat
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        private readonly LinkPreviewService _linkPreviewService;
        private readonly VectorDbContext _context;
        private readonly ManagementDbContext _managementContext;
        private readonly OmgevingService _omgevingService;

        public ChatController(ChatService chatService, LinkPreviewService linkPreviewService, VectorDbContext context, ManagementDbContext managementContext, OmgevingService omgevingService)
        {
            _chatService = chatService;
            _linkPreviewService = linkPreviewService;
            _context = context;
            _managementContext = managementContext;
            _omgevingService = omgevingService;
        }

        public async Task<IActionResult> Index(int projectId = 0)
        {
            var (botName, resolvedProjectId, hasProject) = await GetProjectContextAsync(projectId);
            ViewData["BotName"] = botName;
            ViewData["HasProject"] = hasProject;
            ViewData["SelectedProjectId"] = resolvedProjectId;

            if (resolvedProjectId > 0)
            {
                var project = await _context.Projects.FindAsync(resolvedProjectId);
                ViewData["EmbedKey"] = project?.EmbedKey;
            }

            if (User.IsInRole("Admin"))
            {
                var projects = await _context.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();
                ViewData["AdminProjects"] = new SelectList(projects, "ID", "Naam", resolvedProjectId);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            if (User.IsInRole("Admin")) return Forbid();

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();
            if (entry == null) return RedirectToAction("Index");

            var project = await _context.Projects.FindAsync(entry.Project);
            if (project == null) return RedirectToAction("Index");

            return View(project);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(int id, string botName, bool streamingEnabled)
        {
            if (User.IsInRole("Admin")) return Forbid();

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry == null || entry.Project != id) return RedirectToAction("Index");

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return RedirectToAction("Index");

            project.BotName = string.IsNullOrWhiteSpace(botName) ? "Assistant" : botName.Trim();
            project.StreamingEnabled = streamingEnabled;
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Embed(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return NotFound();

            var project = await _context.Projects
                .Include(p => p.WidgetConfig)
                .FirstOrDefaultAsync(p => p.EmbedKey == key && p.Status == 1);

            if (project == null) return NotFound();

            ViewData["BotName"] = project.BotName;
            ViewData["ProjectId"] = project.ID;
            ViewData["EmbedKey"] = key;
            ViewData["WidgetConfig"] = project.WidgetConfig ?? new Models.Entities.WidgetConfig();
            ViewData["StreamingEnabled"] = project.StreamingEnabled;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AskEmbed([FromQuery] string key, [FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Missing embed key.");

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.EmbedKey == key && p.Status == 1);

            if (project == null)
                return BadRequest("Invalid embed key.");

            var projectId = project.ID;
            var botName = project.BotName;

            // Zoek of maak een conversatie op basis van het session token.
            // Gebruiker blijft altijd null voor widget-gesprekken — geen persoonsgebonden ID.
            var sessionToken = request.SessionToken?.Trim();
            Conversation? existingConversation = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                existingConversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.SessionToken == sessionToken && c.BronType == "widget");
            }

            if (existingConversation == null)
            {
                sessionToken = Guid.NewGuid().ToString("N");
                existingConversation = new Conversation
                {
                    Gebruiker = null,
                    SessionToken = sessionToken,
                    ProjectID = projectId,
                    BronType = "widget",
                    GemaaktOp = DateTime.Now,
                    GewijzigdOp = DateTime.Now,
                    Status = 1
                };
                _context.Conversations.Add(existingConversation);
                await _context.SaveChangesAsync();
            }

            var conversationId = existingConversation.ID;

            var widgetConfig = await _context.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectID == projectId);

            var escalateCommunication = HasActiveEscalationChannel(project, widgetConfig);

            var response = await _chatService.Ask(request, projectId,
                extraCommunicationEnabled: escalateCommunication);

            var (whatsAppUrl, emailUrl) = BuildEscalationUrls(
                project, widgetConfig, response.GenerativeResponse.TransferToWhatsApp);

            _context.Messages.Add(new Message
            {
                Conversation = conversationId,
                Tekst = request.Query,
                GemaaktOp = DateTime.Now,
                SenderType = 1
            });
            _context.Messages.Add(new Message
            {
                Conversation = conversationId,
                Tekst = response.GenerativeResponse.ResponseText,
                GemaaktOp = DateTime.Now,
                SenderType = 2
            });
            await _context.SaveChangesAsync();

            var viewModel = new ChatPanelViewModel
            {
                BotName = botName,
                ConversationId = conversationId,
                SessionToken = sessionToken   // widget-JS slaat dit op in localStorage
            };

            if (request.History != null && request.History.Any())
                viewModel.Messages.AddRange(request.History);

            viewModel.Messages.Add(new ChatMessage
            {
                Content = request.Query,
                IsResponse = false,
                Timestamp = DateTime.Now
            });

            var rawSourceLinks = response.GenerativeResponse.UsedChunkIds
                .Select(chunkId => response.Chunks.FirstOrDefault(c => c.Chunk.ID == chunkId))
                .Where(rc => rc != null && !string.IsNullOrWhiteSpace(rc.BronLink))
                .Select(rc => new VectorRagDemo.Models.DataContracts.SourceLink
                {
                    Url = rc.BronLink!,
                    FallbackTitle = rc.BronTitle ?? rc.Chunk.Bron?.Title ?? "Document"
                })
                .DistinctBy(sl => sl.Url)
                .ToList();

            // Fetch link previews for all source URLs in parallel.
            var previewTasks = rawSourceLinks
                .Select(sl => _linkPreviewService.FetchLinkPreviewAsync(sl.Url));
            var previews = await Task.WhenAll(previewTasks);
            for (int i = 0; i < rawSourceLinks.Count; i++)
                rawSourceLinks[i].Preview = previews[i];

            viewModel.Messages.Add(new ChatMessage
            {
                Content = response.GenerativeResponse.ResponseText,
                Context = response.GenerativeResponse.SourceText,
                SourceLinks = rawSourceLinks,
                WhatsAppUrl = whatsAppUrl,
                WhatsAppCtaText = widgetConfig?.WhatsAppCtaText,
                WhatsAppButtonText = widgetConfig?.WhatsAppButtonText,
                EmailUrl = emailUrl,
                EmailCtaText = widgetConfig?.EmailCtaText,
                EmailButtonText = widgetConfig?.EmailButtonText,
                IsResponse = true,
                Timestamp = DateTime.Now
            });

            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };

            viewModel.RetrievedChunks = response.Chunks.Select(retrievedChunk =>
            {
                var entity = retrievedChunk.Chunk;
                var chunkData = new
                {
                    entity.ID,
                    entity.BronID,
                    entity.Tekst,
                    entity.GemaaktOp,
                    entity.GewijzigdOp,
                    entity.Status,
                    BronTitle = entity.Bron?.Title
                };

                return JsonSerializer.Serialize(new
                {
                    Chunk = chunkData,
                    retrievedChunk.Freshness,
                    retrievedChunk.InitialSimilirityScore,
                    retrievedChunk.BronLink,
                    retrievedChunk.BronTitle
                }, jsonOptions);
            }).ToList();

            return PartialView("_ChatPanel", viewModel);
        }

        /// <summary>
        /// Streaming variant of AskEmbed. Returns an SSE stream with individual token events
        /// followed by a final "done" event containing metadata (conversationId, chatHistory, etc.).
        /// Used when project.StreamingEnabled is true.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task AskEmbedStream([FromQuery] string key, [FromBody] ChatRequest request, CancellationToken ct)
        {
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.Headers["Connection"] = "keep-alive";

            async Task WriteEventAsync(string data)
            {
                await Response.WriteAsync($"data: {data}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                await WriteEventAsync("{\"type\":\"error\",\"message\":\"Missing embed key.\"}");
                return;
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.EmbedKey == key && p.Status == 1, ct);

            if (project == null)
            {
                await WriteEventAsync("{\"type\":\"error\",\"message\":\"Invalid embed key.\"}");
                return;
            }

            var projectId = project.ID;
            var botName = project.BotName;

            // Session / conversation handling — identical to AskEmbed
            var sessionToken = request.SessionToken?.Trim();
            Conversation? existingConversation = null;

            if (!string.IsNullOrEmpty(sessionToken))
            {
                existingConversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.SessionToken == sessionToken && c.BronType == "widget", ct);
            }

            if (existingConversation == null)
            {
                sessionToken = Guid.NewGuid().ToString("N");
                existingConversation = new Conversation
                {
                    Gebruiker = null,
                    SessionToken = sessionToken,
                    ProjectID = projectId,
                    BronType = "widget",
                    GemaaktOp = DateTime.Now,
                    GewijzigdOp = DateTime.Now,
                    Status = 1
                };
                _context.Conversations.Add(existingConversation);
                await _context.SaveChangesAsync(ct);
            }

            var conversationId = existingConversation.ID;
            var widgetConfig = await _context.WidgetConfigs.FirstOrDefaultAsync(w => w.ProjectID == projectId, ct);
            var escalateCommunication = HasActiveEscalationChannel(project, widgetConfig);

            // Run preparation (pre-processing, embedding, vector search, placeholders)
            Services.ChatPreparation prep;
            try
            {
                prep = await _chatService.PrepareAsync(request, projectId);
            }
            catch (Exception ex)
            {
                await WriteEventAsync($"{{\"type\":\"error\",\"message\":{System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}");
                return;
            }

            // Run summarization — also detects transferToWhatsApp when escalation is active,
            // so the result is available before streaming starts.
            (string RelevantOutput, List<int> UsedChunkIds, bool TransferToWhatsApp) summarized;
            try
            {
                summarized = await _chatService.SummarizeAsync(prep, projectId, escalateCommunication);
            }
            catch (Exception ex)
            {
                await WriteEventAsync($"{{\"type\":\"error\",\"message\":{System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}");
                return;
            }

            // Stream final response tokens
            var fullResponse = new System.Text.StringBuilder();
            try
            {
                await foreach (var token in _chatService.StreamFinalResponseAsync(prep, summarized.RelevantOutput, projectId, ct))
                {
                    fullResponse.Append(token);
                    await WriteEventAsync(System.Text.Json.JsonSerializer.Serialize(new { type = "token", content = token }));
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                await WriteEventAsync($"{{\"type\":\"error\",\"message\":{System.Text.Json.JsonSerializer.Serialize(ex.Message)}}}");
                return;
            }

            if (ct.IsCancellationRequested) return;

            var responseText = fullResponse.ToString();

            // Build escalation URLs (same logic as AskEmbed)
            var (whatsAppUrl, emailUrl) = BuildEscalationUrls(project, widgetConfig, summarized.TransferToWhatsApp);

            // Build source links
            var rawSourceLinks = summarized.UsedChunkIds
                .Select(chunkId => prep.RetrievedChunks.FirstOrDefault(c => c.Chunk.ID == chunkId))
                .Where(rc => rc != null && !string.IsNullOrWhiteSpace(rc!.BronLink))
                .Select(rc => new Models.DataContracts.SourceLink
                {
                    Url = rc!.BronLink!,
                    FallbackTitle = rc.BronTitle ?? rc.Chunk.Bron?.Title ?? "Document"
                })
                .DistinctBy(sl => sl.Url)
                .ToList();

            var previewTasks = rawSourceLinks.Select(sl => _linkPreviewService.FetchLinkPreviewAsync(sl.Url));
            var previews = await Task.WhenAll(previewTasks);
            for (int i = 0; i < rawSourceLinks.Count; i++)
                rawSourceLinks[i].Preview = previews[i];

            // Build updated chat history for the client
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };

            var updatedHistory = new List<Models.DataContracts.ChatMessage>();
            if (request.History != null) updatedHistory.AddRange(request.History);
            updatedHistory.Add(new Models.DataContracts.ChatMessage { Content = request.Query, IsResponse = false, Timestamp = DateTime.Now });
            updatedHistory.Add(new Models.DataContracts.ChatMessage
            {
                Content = responseText,
                IsResponse = true,
                Timestamp = DateTime.Now,
                Context = summarized.RelevantOutput,
                WhatsAppUrl = whatsAppUrl,
                WhatsAppCtaText = widgetConfig?.WhatsAppCtaText,
                WhatsAppButtonText = widgetConfig?.WhatsAppButtonText,
                EmailUrl = emailUrl,
                EmailCtaText = widgetConfig?.EmailCtaText,
                EmailButtonText = widgetConfig?.EmailButtonText
            });

            var retrievedChunksJson = prep.RetrievedChunks.Select(rc =>
            {
                var entity = rc.Chunk;
                var chunkData = new
                {
                    entity.ID,
                    entity.BronID,
                    entity.Tekst,
                    entity.GemaaktOp,
                    entity.GewijzigdOp,
                    entity.Status,
                    BronTitle = entity.Bron?.Title
                };
                return JsonSerializer.Serialize(new
                {
                    Chunk = chunkData,
                    rc.Freshness,
                    rc.InitialSimilirityScore,
                    rc.BronLink,
                    rc.BronTitle
                }, jsonOptions);
            }).ToList();

            // Save messages to database
            _context.Messages.Add(new Message
            {
                Conversation = conversationId,
                Tekst = request.Query,
                GemaaktOp = DateTime.Now,
                SenderType = 1
            });
            _context.Messages.Add(new Message
            {
                Conversation = conversationId,
                Tekst = responseText,
                GemaaktOp = DateTime.Now,
                SenderType = 2
            });
            await _context.SaveChangesAsync(ct);

            // Send final "done" event with all metadata the client needs to update state
            var donePayload = new
            {
                type = "done",
                conversationId,
                sessionToken,
                usedChunkIds = summarized.UsedChunkIds,
                context = summarized.RelevantOutput,
                chatHistoryJson = JsonSerializer.Serialize(updatedHistory, jsonOptions),
                retrievedChunksJson = JsonSerializer.Serialize(retrievedChunksJson, jsonOptions),
                sourceLinks = rawSourceLinks.Select(sl => new
                {
                    url = sl.Url,
                    fallbackTitle = sl.FallbackTitle,
                    preview = sl.Preview == null ? null : new
                    {
                        title = sl.Preview.Title,
                        description = sl.Preview.Description,
                        imageUrl = sl.Preview.ImageUrl,
                        faviconUrl = sl.Preview.FaviconUrl,
                        siteName = sl.Preview.SiteName
                    }
                }).ToList(),
                // Escalation — null when not triggered
                whatsAppUrl,
                whatsAppCtaText = whatsAppUrl != null ? widgetConfig?.WhatsAppCtaText : null,
                whatsAppButtonText = whatsAppUrl != null ? widgetConfig?.WhatsAppButtonText : null,
                emailUrl,
                emailCtaText = emailUrl != null ? widgetConfig?.EmailCtaText : null,
                emailButtonText = emailUrl != null ? widgetConfig?.EmailButtonText : null,
                botName,
                timestamp = DateTime.Now.ToString("HH:mm")
            };

            await WriteEventAsync(JsonSerializer.Serialize(donePayload, jsonOptions));
            await WriteEventAsync("[DONE]");
        }

        // Verwijdert een widget-gesprek op basis van session token (GDPR Art. 17).
        // Aanroepbaar zonder login — de token IS de autorisatie.
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> DeleteWidgetConversation([FromQuery] string key, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(token))
                return BadRequest("Ongeldige aanvraag.");

            // Valideer embed key zodat willekeurige tokens niet verwijderd kunnen worden
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.EmbedKey == key && p.Status == 1);

            if (project == null)
                return BadRequest("Onbekende widget.");

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.SessionToken == token && c.BronType == "widget");

            if (conversation == null)
                return NotFound();

            await _context.Messages
                .Where(m => m.Conversation == conversation.ID)
                .ExecuteDeleteAsync();

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> WidgetSettings(int projectId = 0)
        {
            var project = await ResolveProjectForSettingsAsync(projectId);
            if (project == null) return RedirectToAction("Index");

            await _context.Entry(project).Reference(p => p.WidgetConfig).LoadAsync();
            var config = project.WidgetConfig ?? new Models.Entities.WidgetConfig { ProjectID = project.ID };

            ViewData["ProjectId"] = project.ID;
            ViewData["ProjectName"] = project.Naam;
            ViewData["ExtraCommunicationEnabled"] = project.ExtraCommunicationEnabled;

            if (User.IsInRole("Admin"))
            {
                var projects = await _context.Projects
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.Naam)
                    .ToListAsync();
                ViewData["AdminProjects"] = new SelectList(projects, "ID", "Naam", project.ID);
            }

            return View(config);
        }

        [HttpPost]
        public async Task<IActionResult> WidgetSettings(int projectId, Models.Entities.WidgetConfig config)
        {
            var project = await ResolveProjectForSettingsAsync(projectId);
            if (project == null) return RedirectToAction("Index");

            await _context.Entry(project).Reference(p => p.WidgetConfig).LoadAsync();

            // Coerce nullable string fields to empty string
            config.GreetingMessage    = config.GreetingMessage    ?? string.Empty;
            config.HeaderTitle        = config.HeaderTitle        ?? string.Empty;
            config.HeaderLogoUrl      = config.HeaderLogoUrl      ?? string.Empty;
            config.ButtonLogoUrl      = config.ButtonLogoUrl      ?? string.Empty;
            config.ButtonShape        = config.ButtonShape        ?? "circle";
            config.ButtonBorderColor  = config.ButtonBorderColor  ?? "#ffffff";
            config.HideSide           = config.HideSide           ?? "right";
            config.IntroMessage       = config.IntroMessage       ?? string.Empty;
            config.ButtonSvgIdle      = config.ButtonSvgIdle      ?? string.Empty;
            config.ButtonSvgPeek      = config.ButtonSvgPeek      ?? string.Empty;
            config.ButtonSvgOpen      = config.ButtonSvgOpen      ?? string.Empty;
            config.WhatsAppNumber     = config.WhatsAppNumber     ?? string.Empty;
            config.WhatsAppButtonText = config.WhatsAppButtonText ?? "Chat via WhatsApp";
            config.WhatsAppCtaText    = config.WhatsAppCtaText    ?? "Neem gelijk contact op met mijn collega";
            config.EmailAddress       = config.EmailAddress       ?? string.Empty;
            config.EmailSubject       = config.EmailSubject       ?? "Vraag via chat";
            config.EmailButtonText    = config.EmailButtonText    ?? "Stuur een e-mail";
            config.EmailCtaText       = config.EmailCtaText       ?? "Neem gelijk contact op met mijn collega";

            if (project.WidgetConfig == null)
            {
                config.ID = 0;
                config.ProjectID = project.ID;
                config.ExtraCommunicationEnabled = false; // master switch lives on Project, not WidgetConfig
                _context.WidgetConfigs.Add(config);
            }
            else
            {
                var existing = project.WidgetConfig;
                existing.WidgetPosition    = config.WidgetPosition;
                existing.OffsetX           = config.OffsetX;
                existing.OffsetY           = config.OffsetY;
                existing.ButtonColor       = config.ButtonColor;
                existing.ButtonSize        = config.ButtonSize;
                existing.ButtonShape       = config.ButtonShape;
                existing.ButtonBorderWidth = Math.Clamp(config.ButtonBorderWidth, 0, 8);
                existing.ButtonBorderColor = config.ButtonBorderColor;
                existing.ButtonIconPadding = Math.Clamp(config.ButtonIconPadding, 0, 30);
                existing.PopupWidth        = config.PopupWidth;
                existing.PopupHeight       = config.PopupHeight;
                existing.PopupBorderRadius = config.PopupBorderRadius;
                existing.HeaderBgColor     = config.HeaderBgColor;
                existing.HeaderTextColor   = config.HeaderTextColor;
                existing.UserBubbleBgColor   = config.UserBubbleBgColor;
                existing.UserBubbleTextColor = config.UserBubbleTextColor;
                existing.BotBubbleBgColor    = config.BotBubbleBgColor;
                existing.BotBubbleTextColor  = config.BotBubbleTextColor;
                existing.WidgetFontSize    = config.WidgetFontSize;
                existing.GreetingMessage   = config.GreetingMessage;
                existing.HeaderTitle       = config.HeaderTitle;
                existing.HeaderLogoUrl     = config.HeaderLogoUrl;
                existing.ChatBodyBgColor   = config.ChatBodyBgColor;
                existing.InputAreaBgColor  = config.InputAreaBgColor;
                existing.SendButtonColor   = config.SendButtonColor;
                existing.SendButtonIconColor = config.SendButtonIconColor;
                existing.ButtonLogoUrl     = config.ButtonLogoUrl;
                existing.IconHideable      = config.IconHideable;
                existing.HideSide          = config.HideSide;
                existing.PeekAmount        = Math.Clamp(config.PeekAmount, 20, 70);
                existing.ButtonSvgIdle     = config.ButtonSvgIdle;
                existing.ButtonSvgPeek     = config.ButtonSvgPeek;
                existing.ButtonSvgOpen     = config.ButtonSvgOpen;
                existing.IntroMessage      = config.IntroMessage;
                existing.IntroMessageDelay = Math.Clamp(config.IntroMessageDelay, 0, 30);

                // Channel settings (editable by all, but only active when admin enables them)
                existing.WhatsAppEnabled   = config.WhatsAppEnabled;
                existing.WhatsAppNumber    = config.WhatsAppNumber;
                existing.WhatsAppButtonText = config.WhatsAppButtonText;
                existing.WhatsAppCtaText   = config.WhatsAppCtaText;
                existing.EmailEnabled      = config.EmailEnabled;
                existing.EmailAddress      = config.EmailAddress;
                existing.EmailSubject      = config.EmailSubject;
                existing.EmailButtonText   = config.EmailButtonText;
                existing.EmailCtaText      = config.EmailCtaText;
            }

            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Widget-instellingen opgeslagen.";
            return RedirectToAction("WidgetSettings", new { projectId = project.ID });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetWidgetConfig(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return NotFound();

            var project = await _context.Projects
                .Include(p => p.WidgetConfig)
                .FirstOrDefaultAsync(p => p.EmbedKey == key && p.Status == 1);

            if (project == null) return NotFound();

            var cfg = project.WidgetConfig ?? new Models.Entities.WidgetConfig();
            return Json(new
            {
                cfg.WidgetPosition,
                cfg.OffsetX,
                cfg.OffsetY,
                cfg.ButtonColor,
                cfg.ButtonSize,
                cfg.ButtonShape,
                cfg.ButtonBorderWidth,
                cfg.ButtonBorderColor,
                cfg.ButtonIconPadding,
                cfg.ButtonLogoUrl,
                cfg.PopupWidth,
                cfg.PopupHeight,
                cfg.PopupBorderRadius,
                cfg.IconHideable,
                cfg.HideSide,
                cfg.PeekAmount,
                cfg.ButtonSvgIdle,
                cfg.ButtonSvgPeek,
                cfg.ButtonSvgOpen,
                cfg.IntroMessage,
                cfg.IntroMessageDelay
            });
        }

        /// <summary>
        /// Registreert een escalatie-klik (WhatsApp/e-mail doorverwijzing).
        /// Mag anoniem worden aangeroepen — de conversatieID is de enige context die nodig is.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LogEscalation([FromQuery] int conversationId, [FromQuery] string kanaal)
        {
            if (conversationId <= 0 || string.IsNullOrWhiteSpace(kanaal))
                return Ok();

            var normalized = kanaal.ToLowerInvariant();
            if (normalized != "whatsapp" && normalized != "email")
                return Ok();

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation == null)
                return Ok();

            _context.EscalatieEvents.Add(new Models.Entities.EscalatieEvent
            {
                ConversatieID = conversationId,
                ProjectID = conversation.ProjectID,
                SessionToken = conversation.SessionToken,
                Kanaal = normalized,
                GemaaktOp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok();
        }

        private async Task<Models.Entities.Project?> ResolveProjectForSettingsAsync(int projectId)
        {
            if (User.IsInRole("Admin"))
            {
                if (projectId <= 0) return null;
                return await _context.Projects.FindAsync(projectId);
            }

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            if (entry == null) return null;
            return await _context.Projects.FindAsync(entry.Project);
        }

        /// <summary>
        /// Resolves whichever escalation channels are active for the project.
        /// Returns (whatsAppUrl, emailUrl) — each is null when that channel is not configured.
        /// Both are null when the bot did not signal escalation or ExtraCommunicationEnabled is off.
        /// </summary>
        private static (string? WhatsAppUrl, string? EmailUrl) BuildEscalationUrls(
            Models.Entities.Project? project, Models.Entities.WidgetConfig? cfg, bool botSignalled)
        {
            if (!botSignalled || project == null || !project.ExtraCommunicationEnabled || cfg == null)
                return (null, null);

            string? whatsAppUrl = null;
            if (cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber))
            {
                var number = cfg.WhatsAppNumber.TrimStart('+').Replace(" ", "");
                whatsAppUrl = $"https://wa.me/{number}";
            }

            string? emailUrl = null;
            if (cfg.EmailEnabled && !string.IsNullOrWhiteSpace(cfg.EmailAddress))
            {
                var subject = string.IsNullOrWhiteSpace(cfg.EmailSubject) ? "" : $"?subject={Uri.EscapeDataString(cfg.EmailSubject)}";
                emailUrl = $"mailto:{cfg.EmailAddress}{subject}";
            }

            return (whatsAppUrl, emailUrl);
        }

        /// <summary>True when ExtraCommunicationEnabled (on project) and at least one channel has valid settings.</summary>
        private static bool HasActiveEscalationChannel(Models.Entities.Project? project, Models.Entities.WidgetConfig? cfg)
        {
            bool hasProjectSetting = project != null && project.ExtraCommunicationEnabled;

            bool hasWhatsapp = cfg != null && cfg.WhatsAppEnabled && !string.IsNullOrWhiteSpace(cfg.WhatsAppNumber);
            bool hasEmail = cfg != null && cfg.EmailEnabled && !string.IsNullOrWhiteSpace(cfg.EmailAddress);

            return hasProjectSetting && (hasWhatsapp || hasEmail);
        }

        private async Task<(string BotName, int ProjectId, bool HasProject)> GetProjectContextAsync(int adminProjectId = 0)
        {
            if (User.IsInRole("Admin"))
            {
                var id = _omgevingService.ResolveForAdmin(adminProjectId);
                var botName = "Assistant";
                if (id > 0)
                {
                    var project = await _context.Projects.FindAsync(id);
                    botName = project?.BotName ?? "Assistant";
                }
                return (botName, id, false);
            }

            var userId = User.GetUserId();
            var activeId = await _omgevingService.ResolveForUserAsync(userId);
            if (activeId <= 0) return ("Assistant", 0, false);
            var activeProject = await _context.Projects.FindAsync(activeId);
            return (activeProject?.BotName ?? "Assistant", activeId, true);
        }
    }
}
