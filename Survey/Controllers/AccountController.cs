using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Services;

namespace Survey.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(
            IUserService userService, 
            ILogger<AccountController> logger,
            IConfiguration configuration)
        {
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
        }

        #region Login
        
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new LoginDto();

            var emailFromCookie = Request.Cookies["LoginEmail"];
            if (!string.IsNullOrEmpty(emailFromCookie))
            {
                var user = await _userService.GetUserByEmailAsync(emailFromCookie);
                if (user != null && user.FailedLoginCount >= 3)
                {
                    viewModel.ShowCaptcha = true;
                    viewModel.Email = emailFromCookie;
                }
            }

            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
                return View(model);
            }

            Response.Cookies.Append("LoginEmail", model.Email, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                HttpOnly = true,
                Secure = true
            });

            var result = await _userService.ValidateUserAsync(model.Email, model.Password, model.CaptchaResponse);
            
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                model.ShowCaptcha = result.RequiresCaptcha;
                model.Password = string.Empty;
                
                ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
                
                return View(model);
            }

            var user = result.User!;
            
            Response.Cookies.Delete("LoginEmail");

            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserFullName", user.FullName ?? "User");
            HttpContext.Session.SetString("UserAvatar", user.AvatarUrl ?? "");

            if (model.RememberMe)
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("RememberMe", user.UserId.ToString(), cookieOptions);
            }

            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.RegisterUserAsync(model);
            
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please login.";
            return RedirectToAction("Login", "Account");
        }

        #endregion

        #region Forgot Password
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _userService.GeneratePasswordResetTokenAsync(model.Email, baseUrl);

            // Luôn hiển thị thông báo thành công (bảo mật)
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordDto { Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.ResetPasswordAsync(model.Token, model.Password);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Login");
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            if (Request.Cookies["RememberMe"] != null)
            {
                Response.Cookies.Delete("RememberMe");
            }
            
            if (Request.Cookies["LoginEmail"] != null)
            {
                Response.Cookies.Delete("LoginEmail");
            }

            TempData["InfoMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}