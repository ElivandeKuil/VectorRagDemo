using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VectorRagDemo.BLL;
using VectorRagDemo.DAL;
using VectorRagDemo.Extensions;
using VectorRagDemo.Models.DataContracts;
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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            // Resolve the user's project for chunk filtering.
            // Admin has no restriction (projectId = 0 = all chunks).
            int projectId = 0;
            if (!User.IsInRole("Admin"))
            {
                var userId = User.GetUserId();
                var entry = await _context.GebruikerProjecten
                    .Where(gp => gp.Gebruiker == userId && gp.Status == 1)
                    .FirstOrDefaultAsync();

                if (entry == null)
                {
                    // No project assigned — block the request rather than leaking all chunks.
                    return PartialView("_ChatPanel", new ChatPanelViewModel
                    {
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Content = request.Query, IsResponse = false, Timestamp = DateTime.Now },
                            new ChatMessage { Content = "Er is geen project gekoppeld aan uw account. Neem contact op met de beheerder.", IsResponse = true, Timestamp = DateTime.Now }
                        }
                    });
                }

                projectId = entry.Project;
            }

            var response = await _chatService.Ask(request, projectId);

            var viewModel = new ChatPanelViewModel();

            // Add history
            if (request.History != null && request.History.Any())
            {
                viewModel.Messages.AddRange(request.History);
            }

            // Add user message
            viewModel.Messages.Add(new ChatMessage
            {
                Content = request.Query,
                IsResponse = false,
                Timestamp = DateTime.Now
            });

            // Add assistant response
            var assistantMessage = new ChatMessage
            {
                Content = response.GenerativeResponse.ResponseText,
                Context = response.GenerativeResponse.SourceText,
                RedirectUrl = "",
                IsResponse = true,
                Timestamp = DateTime.Now
            };

            // Fetch link preview metadata if redirect URL exists
            if (!string.IsNullOrWhiteSpace(""))
            {
                assistantMessage.LinkPreview = await _linkPreviewService.FetchLinkPreviewAsync("");
            }

            viewModel.Messages.Add(assistantMessage);

            // Serialize retrieved chunks for the view
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
    }
}
