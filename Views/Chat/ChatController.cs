using Microsoft.AspNetCore.Mvc;
using VectorRagDemo.Models;
using VectorRagDemo.Services;

namespace VectorRagDemo.Controllers
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
            return Json(await _chatService.Ask(request));
        }
    }
}
