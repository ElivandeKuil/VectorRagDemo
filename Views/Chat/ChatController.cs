using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VectorRagDemo.Models.DataContracts;
using VectorRagDemo.Models.Requests;
using VectorRagDemo.Models.ViewModels;
using VectorRagDemo.Services;

namespace VectorRagDemo.Views.Chat
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var response = await _chatService.Ask(request);

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
            viewModel.Messages.Add(new ChatMessage
            {
                Content = response.GenerativeResponse.ResponseText,
                Context = response.GenerativeResponse.SourceText,
                IsResponse = true,
                Timestamp = DateTime.Now
            });

            // Serialize retrieved chunks for the view (complete chunk objects)
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };

            viewModel.RetrievedChunks = response.Chunks.Select(retrievedChunk =>
            {
                var entity = retrievedChunk.Chunk;  // Store reference to avoid naming conflict with LINQ Chunk()
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
