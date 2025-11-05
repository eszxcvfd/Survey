using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class SurveyController : Controller
    {
        private readonly ISurveyService _surveyService;
        private readonly ILogger<SurveyController> _logger;

        public SurveyController(ISurveyService surveyService, ILogger<SurveyController> logger)
        {
            _surveyService = surveyService;
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

        #region My Surveys (Dashboard)

        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all")
        {
            _logger.LogInformation("=== Survey Index called with filter: {Filter} ===", filter);

            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in, redirecting to login");
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("Could not get current user ID");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Getting surveys for user: {UserId}", currentUserId.Value);
            var viewModel = await _surveyService.GetMySurveysAsync(currentUserId.Value, filter);
            _logger.LogInformation("Returning view with {Count} surveys", viewModel.Surveys.Count);
            
            return View(viewModel);
        }

        #endregion

        #region Create Survey

        [HttpGet]
        public IActionResult Create()
        {
            _logger.LogInformation("=== Create Survey GET called ===");

            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in");
                return RedirectToAction("Login", "Account");
            }

            return View(new CreateSurveyDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSurveyDto model)
        {
            _logger.LogInformation("=== Create Survey POST called ===");
            _logger.LogInformation("Model Title: {Title}, Description: {Description}, IsAnonymous: {IsAnonymous}", 
                model.Title, model.Description, model.IsAnonymous);

            if (!IsUserLoggedIn())
            {
                _logger.LogWarning("User not logged in");
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                _logger.LogWarning("Could not get current user ID");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Current User ID: {UserId}", currentUserId.Value);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Model error: {Error}", error.ErrorMessage);
                }
                return View(model);
            }

            _logger.LogInformation("ModelState is valid, calling service...");

            try
            {
                var result = await _surveyService.CreateSurveyAsync(model, currentUserId.Value);

                _logger.LogInformation("Service returned: Success={Success}, Message={Message}", 
                    result.Success, result.Message);

                if (!result.Success)
                {
                    _logger.LogError("Service returned failure: {Message}", result.Message);
                    ModelState.AddModelError(string.Empty, result.Message);
                    TempData["ErrorMessage"] = result.Message;
                    return View(model);
                }

                _logger.LogInformation("Survey created successfully, redirecting to Index");
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Create action: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Unexpected error: {ex.Message}");
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return View(model);
            }
        }

        #endregion

        #region Delete Survey

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid surveyId)
        {
            _logger.LogInformation("=== Delete Survey called for {SurveyId} ===", surveyId);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _surveyService.DeleteSurveyAsync(surveyId, currentUserId.Value);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region Survey Settings

        [HttpGet]
        public async Task<IActionResult> Settings(Guid id)
        {
            _logger.LogInformation("=== Survey Settings GET called for {SurveyId} ===", id);

            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _surveyService.GetSurveySettingsAsync(id, currentUserId.Value);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Survey not found or you don't have permission to edit it";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SurveySettingsViewModel model)
        {
            _logger.LogInformation("=== Survey Settings POST called for {SurveyId} ===", model.SurveyId);

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
                _logger.LogWarning("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Model error: {Error}", error.ErrorMessage);
                }
                return View(model);
            }

            var result = await _surveyService.UpdateSurveySettingsAsync(model, currentUserId.Value);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            // Changed: Redirect to Index instead of Settings
            return RedirectToAction("Index");
        }

        #endregion
    }
}