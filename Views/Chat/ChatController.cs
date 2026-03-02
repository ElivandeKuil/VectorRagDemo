using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Entities;
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

        public ChatController(ChatService chatService, LinkPreviewService linkPreviewService, VectorDbContext context)
        {
            _chatService = chatService;
            _linkPreviewService = linkPreviewService;
            _context = context;
        }

        public async Task<IActionResult> Index(int projectId = 0)
        {
            var (botName, resolvedProjectId, hasProject) = await GetProjectContextAsync(projectId);
            ViewData["BotName"] = botName;
            ViewData["HasProject"] = hasProject;
            ViewData["SelectedProjectId"] = resolvedProjectId;

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

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var (botName, projectId, _) = await GetProjectContextAsync(request.ProjectId);

            if (!User.IsInRole("Admin") && projectId == 0)
            {
                return PartialView("_ChatPanel", new ChatPanelViewModel
                {
                    BotName = botName,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Content = request.Query, IsResponse = false, Timestamp = DateTime.Now },
                        new ChatMessage { Content = "Er is geen project gekoppeld aan uw account. Neem contact op met de beheerder.", IsResponse = true, Timestamp = DateTime.Now }
                    }
                });
            }

            var response = await _chatService.Ask(request, projectId);

            var viewModel = new ChatPanelViewModel { BotName = botName };

            if (request.History != null && request.History.Any())
                viewModel.Messages.AddRange(request.History);

            viewModel.Messages.Add(new ChatMessage
            {
                Content = request.Query,
                IsResponse = false,
                Timestamp = DateTime.Now
            });

            var sourceLinks = response.GenerativeResponse.UsedChunkIds
                .Select(chunkId => response.Chunks.FirstOrDefault(c => c.Chunk.ID == chunkId))
                .Where(rc => rc != null && !string.IsNullOrWhiteSpace(rc.BronLink))
                .Select(rc => new VectorRagDemo.Models.DataContracts.SourceLink
                {
                    Title = rc.BronTitle ?? rc.Chunk.Bron?.Title ?? "Document",
                    Url = rc.BronLink!
                })
                .DistinctBy(sl => sl.Url)
                .ToList();

            var assistantMessage = new ChatMessage
            {
                Content = response.GenerativeResponse.ResponseText,
                Context = response.GenerativeResponse.SourceText,
                SourceLinks = sourceLinks,
                IsResponse = true,
                Timestamp = DateTime.Now
            };

            viewModel.Messages.Add(assistantMessage);

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
                    retrievedChunk.InitialSimilirityScore
                }, jsonOptions);
            }).ToList();

            return PartialView("_ChatPanel", viewModel);
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
        public async Task<IActionResult> Settings(int id, string botName)
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
            project.GewijzigdOp = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        private async Task<(string BotName, int ProjectId, bool HasProject)> GetProjectContextAsync(int adminProjectId = 0)
        {
            if (User.IsInRole("Admin"))
            {
                var botName = "Assistant";
                if (adminProjectId > 0)
                {
                    var project = await _context.Projects.FindAsync(adminProjectId);
                    botName = project?.BotName ?? "Assistant";
                }
                return (botName, adminProjectId, false);
            }

            var userId = User.GetUserId();
            var entry = await _context.GebruikerProjecten
                .Include(gp => gp.ProjectNavigation)
                .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                .FirstOrDefaultAsync();

            return (entry?.ProjectNavigation?.BotName ?? "Assistant", entry?.Project ?? 0, entry != null);
        }
    }
}
