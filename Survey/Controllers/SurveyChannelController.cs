using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class SurveyChannelController : Controller
    {
        private readonly ISurveyChannelService _channelService;
        private readonly ILogger<SurveyChannelController> _logger;

        public SurveyChannelController(
            ISurveyChannelService channelService,
            ILogger<SurveyChannelController> logger)
        {
            _channelService = channelService;
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
        public async Task<IActionResult> Manage(Guid surveyId)
        {
            _logger.LogInformation("=== Manage Channels called for survey {SurveyId} ===", surveyId);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _channelService.GetChannelsForSurveyAsync(surveyId, currentUserId.Value);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Survey not found or you don't have access";
                return RedirectToAction("Index", "Survey");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateChannelDto model)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data provided";
                return RedirectToAction("Manage", new { surveyId = model.SurveyId });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _channelService.CreateChannelAsync(model, currentUserId.Value, baseUrl);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Manage", new { surveyId = model.SurveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid channelId, Guid surveyId)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _channelService.DeleteChannelAsync(channelId, currentUserId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Manage", new { surveyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid channelId, Guid surveyId)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _channelService.ToggleChannelStatusAsync(channelId, currentUserId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Manage", new { surveyId });
        }
    }
}