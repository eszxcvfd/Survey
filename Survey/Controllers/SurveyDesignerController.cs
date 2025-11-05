using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class SurveyDesignerController : Controller
    {
        private readonly ISurveyDesignerService _designerService;
        private readonly ILogger<SurveyDesignerController> _logger;

        public SurveyDesignerController(ISurveyDesignerService designerService, ILogger<SurveyDesignerController> logger)
        {
            _designerService = designerService;
            _logger = logger;
        }

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        private Guid? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid id)
        {
            _logger.LogInformation("=== Survey Designer Index called for survey {SurveyId} ===", id);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _designerService.GetSurveyForDesignerAsync(id, currentUserId.Value);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Survey not found or you don't have permission to edit it";
                return RedirectToAction("Index", "Survey");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion([FromBody] AddQuestionRequest request)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var result = await _designerService.AddQuestionAsync(request.SurveyId, request.QuestionType, currentUserId.Value);

            if (result.Success)
            {
                return Json(new { success = true, question = result.Data, message = result.Message });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionViewModel model)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var result = await _designerService.UpdateQuestionAsync(model, currentUserId.Value);

            if (result.Success)
            {
                return Json(new { success = true, question = result.Data, message = result.Message });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion([FromBody] DeleteQuestionRequest request)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var result = await _designerService.DeleteQuestionAsync(request.QuestionId, currentUserId.Value);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> ReorderQuestions([FromBody] ReorderQuestionsRequest request)
        {
            if (!IsUserLoggedIn())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var result = await _designerService.ReorderQuestionsAsync(request.SurveyId, request.QuestionIds, currentUserId.Value);

            return Json(new { success = result.Success, message = result.Message });
        }

        public class AddQuestionRequest
        {
            public Guid SurveyId { get; set; }
            public string QuestionType { get; set; } = string.Empty;
        }

        public class DeleteQuestionRequest
        {
            public Guid QuestionId { get; set; }
        }

        public class ReorderQuestionsRequest
        {
            public Guid SurveyId { get; set; }
            public List<Guid> QuestionIds { get; set; } = new();
        }
    }
}