using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class ManageController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<ManageController> _logger;

        public ManageController(IUserService userService, ILogger<ManageController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // Check if user is logged in
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

        #region Profile Management

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index", "Home");
            }

            var model = new EditProfileDto
            {
                FullName = user.FullName ?? string.Empty,
                CurrentAvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileDto model)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.UpdateProfileAsync(userId.Value, model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Update session
            var updatedUser = await _userService.GetUserByIdAsync(userId.Value);
            if (updatedUser != null)
            {
                HttpContext.Session.SetString("UserFullName", updatedUser.FullName ?? "User");
                HttpContext.Session.SetString("UserAvatar", updatedUser.AvatarUrl ?? "");
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Profile");
        }

        #endregion

        #region Change Password

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Profile");
        }

        #endregion
    }
}