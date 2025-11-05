using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class SurveyCollaboratorController : Controller
    {
        private readonly ISurveyCollaboratorService _collaboratorService;
        private readonly ILogger<SurveyCollaboratorController> _logger;

        public SurveyCollaboratorController(
            ISurveyCollaboratorService collaboratorService,
            ILogger<SurveyCollaboratorController> logger)
        {
            _collaboratorService = collaboratorService;
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
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await _collaboratorService.GetCollaboratorsForSurveyAsync(surveyId, currentUserId.Value);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Survey not found";
                return RedirectToAction("Index", "Home");
            }

            // Only owner can manage collaborators
            if (!model.IsOwner)
            {
                TempData["ErrorMessage"] = "Only the survey owner can manage collaborators";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddCollaboratorDto model)
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

            var result = await _collaboratorService.AddCollaboratorAsync(
                model.SurveyId,
                model.Email,
                model.Role,
                currentUserId.Value
            );

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
        public async Task<IActionResult> Remove(Guid surveyId, Guid userId)
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

            var result = await _collaboratorService.RemoveCollaboratorAsync(
                surveyId,
                userId,
                currentUserId.Value
            );

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
        public async Task<IActionResult> UpdateRole(UpdateCollaboratorRoleDto model)
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

            var result = await _collaboratorService.UpdateRoleAsync(
                model.SurveyId,
                model.UserId,
                model.NewRole,
                currentUserId.Value
            );

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
    }
}