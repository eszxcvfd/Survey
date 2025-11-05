using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class BranchLogicController : Controller
    {
        private readonly IBranchLogicService _logicService;
        private readonly ILogger<BranchLogicController> _logger;

        public BranchLogicController(IBranchLogicService logicService, ILogger<BranchLogicController> logger)
        {
            _logicService = logicService;
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
            _logger.LogInformation("=== Branch Logic Index called for survey {SurveyId} ===", id);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _logicService.GetLogicForSurveyAsync(id, currentUserId.Value);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Survey not found or you don't have permission to manage logic";
                return RedirectToAction("Index", "Survey");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRule(AddRuleViewModel model)
        {
            if (!IsUserLoggedIn())
            {
                TempData["ErrorMessage"] = "Not authenticated";
                return RedirectToAction("Index", new { id = model.SurveyId });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["ErrorMessage"] = "Not authenticated";
                return RedirectToAction("Index", new { id = model.SurveyId });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join(", ", errors);
                return RedirectToAction("Index", new { id = model.SurveyId });
            }

            var result = await _logicService.AddRuleAsync(model, currentUserId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index", new { id = model.SurveyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRule([FromBody] DeleteRuleRequest request)
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

            var result = await _logicService.DeleteRuleAsync(request.LogicId, currentUserId.Value);

            return Json(new { success = result.Success, message = result.Message });
        }

        public class DeleteRuleRequest
        {
            public Guid LogicId { get; set; }
        }
    }
}