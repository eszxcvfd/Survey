using System.Security.Cryptography;
using System.Text;
using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICaptchaService _captchaService;
        private readonly IEmailService _emailService;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;
        private readonly IWebHostEnvironment _environment;

        public UserService(
            IUserRepository userRepository, 
            ICaptchaService captchaService,
            IEmailService emailService,
            ITokenGenerator tokenGenerator,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger,
            IWebHostEnvironment environment)
        {
            _userRepository = userRepository;
            _captchaService = captchaService;
            _emailService = emailService;
            _tokenGenerator = tokenGenerator;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _environment = environment;
        }

        public async Task<ServiceResult<User>> RegisterUserAsync(RegisterDto model)
        {
            // 1. Kiểm tra email đã tồn tại
            var existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return ServiceResult<User>.FailureResult("Email already exists");
            }

            // 2. Hash password using PBKDF2
            var passwordHash = _passwordHasher.Hash(model.Password);

            // 3. Tạo User entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = model.Email,
                PasswordHash = Encoding.UTF8.GetBytes(passwordHash), // Convert string to bytes for storage
                FullName = model.FullName,
                IsActive = true,
                FailedLoginCount = 0,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // 4. Lưu vào database
            try
            {
                await _userRepository.AddAsync(newUser);
                return ServiceResult<User>.SuccessResult(newUser, "User registered successfully");
            }
            catch (Exception ex)
            {
                return ServiceResult<User>.FailureResult($"Error registering user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<User>> AuthenticateAsync(LoginDto model)
        {
            var result = await ValidateUserAsync(model.Email, model.Password, model.CaptchaResponse);
            
            if (!result.IsSuccess)
            {
                return ServiceResult<User>.FailureResult(result.ErrorMessage);
            }

            return ServiceResult<User>.SuccessResult(result.User!, "Login successful");
        }

        public async Task<LoginResult> ValidateUserAsync(string email, string password, string? captchaResponse)
        {
            // 1. Lấy User theo email
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                return LoginResult.Failure("Invalid email or password");
            }

            // 2. Kiểm tra tài khoản có bị khóa không (Quy tắc 5 lần)
            if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow)
            {
                var lockoutMinutesRemaining = (user.LockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes;
                _logger.LogWarning("Login attempt for locked account: {Email}. Locked until: {LockoutEnd}", 
                    email, user.LockedUntilUtc.Value);
                
                return LoginResult.Failure(
                    $"Account is locked. Please try again in {Math.Ceiling(lockoutMinutesRemaining)} minute(s).",
                    requiresCaptcha: true,
                    lockoutEndUtc: user.LockedUntilUtc.Value
                );
            }

            // 3. Kiểm tra CAPTCHA (Quy tắc 3 lần)
            if (user.FailedLoginCount >= 3)
            {
                var isCaptchaValid = await _captchaService.ValidateAsync(captchaResponse);
                if (!isCaptchaValid)
                {
                    _logger.LogWarning("Invalid CAPTCHA for user: {Email}", email);
                    return LoginResult.Failure("Invalid CAPTCHA. Please try again.", requiresCaptcha: true);
                }
            }

            // 4. Kiểm tra mật khẩu using PBKDF2
            var storedHash = Encoding.UTF8.GetString(user.PasswordHash);
            var isPasswordValid = _passwordHasher.Verify(storedHash, password);

            // 5. Xử lý kết quả
            if (isPasswordValid)
            {
                // THÀNH CÔNG - Reset bộ đếm
                if (user.FailedLoginCount > 0 || user.LockedUntilUtc.HasValue)
                {
                    user.FailedLoginCount = 0;
                    user.LockedUntilUtc = null;
                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation("User {Email} login successful. Failed count reset.", email);
                }

                return LoginResult.Success(user);
            }
            else
            {
                // THẤT BẠI - Tăng bộ đếm
                user.FailedLoginCount++;
                _logger.LogWarning("Failed login attempt for {Email}. Count: {Count}", email, user.FailedLoginCount);

                // Áp dụng Quy tắc 5 lần - Khóa tài khoản
                if (user.FailedLoginCount >= 5)
                {
                    user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15);
                    await _userRepository.UpdateAsync(user);
                    
                    _logger.LogWarning("Account {Email} locked until {LockoutEnd}", email, user.LockedUntilUtc.Value);
                    
                    return LoginResult.Failure(
                        "Too many failed attempts. Your account has been locked for 15 minutes.",
                        requiresCaptcha: true,
                        lockoutEndUtc: user.LockedUntilUtc.Value
                    );
                }

                await _userRepository.UpdateAsync(user);

                // Yêu cầu CAPTCHA từ lần thứ 3 trở đi
                bool requiresCaptcha = user.FailedLoginCount >= 3;
                
                return LoginResult.Failure(
                    "Invalid email or password",
                    requiresCaptcha: requiresCaptcha
                );
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepository.GetUserByIdAsync(userId);
        }

        public async Task<ServiceResult> GeneratePasswordResetTokenAsync(string email, string baseUrl)
        {
            _logger.LogInformation("Password reset requested for email: {Email}", email);

            var user = await _userRepository.GetUserByEmailAsync(email);
            
            // QUAN TRỌNG: Không tiết lộ email có tồn tại hay không (bảo mật)
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                // Vẫn trả về success để không lộ thông tin
                return ServiceResult.SuccessResult("If the email exists, a password reset link has been sent.");
            }

            // Tạo token an toàn
            var token = _tokenGenerator.GenerateSecureToken();

            // Cập nhật user
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token hết hạn sau 1 giờ

            await _userRepository.UpdateAsync(user);

            // Tạo reset link
            var resetLink = $"{baseUrl}/Account/ResetPassword?token={token}";

            // Tạo HTML email
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Hello {user.FullName ?? "User"},</p>
                    <p>We received a request to reset your password. Click the button below to reset it:</p>
                    <p style='margin: 20px 0;'>
                        <a href='{resetLink}' 
                           style='background-color: #4CAF50; color: white; padding: 12px 24px; 
                                  text-decoration: none; border-radius: 4px; display: inline-block;'>
                            Reset Password
                        </a>
                    </p>
                    <p>Or copy and paste this link into your browser:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>
                    <p><strong>This link will expire in 1 hour.</strong></p>
                    <p>If you didn't request this password reset, please ignore this email.</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>
                        This is an automated email. Please do not reply.
                    </p>
                </body>
                </html>";

            // Gửi email
            var emailSent = await _emailService.SendEmailAsync(
                user.Email,
                "Password Reset Request - Survey App",
                emailBody
            );

            if (!emailSent)
            {
                _logger.LogError("Failed to send password reset email to {Email}", email);
                return ServiceResult.FailureResult("Failed to send reset email. Please try again later.");
            }

            _logger.LogInformation("Password reset email sent successfully to {Email}", email);
            return ServiceResult.SuccessResult("If the email exists, a password reset link has been sent.");
        }

        public async Task<ServiceResult> ResetPasswordAsync(string token, string newPassword)
        {
            _logger.LogInformation("Attempting password reset with token");

            // Tìm user bằng token
            var user = await _userRepository.GetUserByResetTokenAsync(token);

            // Xác thực token
            if (user == null || user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired reset token");
                return ServiceResult.FailureResult("Invalid or expired reset token. Please request a new password reset.");
            }

            // Hash mật khẩu mới using PBKDF2
            var newPasswordHash = _passwordHasher.Hash(newPassword);

            // Cập nhật user
            user.PasswordHash = Encoding.UTF8.GetBytes(newPasswordHash);
            user.ResetToken = null; // Vô hiệu hóa token
            user.ResetTokenExpiry = null;
            user.FailedLoginCount = 0; // Reset bộ đếm lỗi
            user.LockedUntilUtc = null; // Mở khóa nếu có
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password reset successful for user {Email}", user.Email);
            return ServiceResult.SuccessResult("Password has been reset successfully. You can now login with your new password.");
        }

        public async Task<ServiceResult> UpdateProfileAsync(Guid userId, EditProfileDto model)
        {
            _logger.LogInformation("Updating profile for user {UserId}", userId);

            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return ServiceResult.FailureResult("User not found");
                }

                // Cập nhật FullName
                user.FullName = model.FullName;

                // Xử lý Avatar nếu có upload file mới
                if (model.AvatarImage != null && model.AvatarImage.Length > 0)
                {
                    // Kiểm tra loại file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(model.AvatarImage.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return ServiceResult.FailureResult("Only image files (jpg, jpeg, png, gif) are allowed");
                    }

                    // Kiểm tra kích thước file (giới hạn 5MB)
                    if (model.AvatarImage.Length > 5 * 1024 * 1024)
                    {
                        return ServiceResult.FailureResult("File size cannot exceed 5MB");
                    }

                    // Tạo tên file unique
                    var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                    
                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Xóa avatar cũ nếu có
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        var oldAvatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                        if (File.Exists(oldAvatarPath))
                        {
                            try
                            {
                                File.Delete(oldAvatarPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete old avatar file");
                            }
                        }
                    }

                    // Lưu file mới
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.AvatarImage.CopyToAsync(stream);
                    }

                    // Cập nhật URL trong database
                    user.AvatarUrl = $"/uploads/avatars/{fileName}";
                    _logger.LogInformation("Avatar uploaded successfully for user {UserId}", userId);
                }

                user.UpdatedAtUtc = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
                return ServiceResult.SuccessResult("Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return ServiceResult.FailureResult($"Error updating profile: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            _logger.LogInformation("Changing password for user {UserId}", userId);

            try
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return ServiceResult.FailureResult("User not found");
                }

                // Kiểm tra mật khẩu hiện tại using PBKDF2
                var storedHash = Encoding.UTF8.GetString(user.PasswordHash);
                var isCurrentPasswordValid = _passwordHasher.Verify(storedHash, currentPassword);

                if (!isCurrentPasswordValid)
                {
                    _logger.LogWarning("Invalid current password for user {UserId}", userId);
                    return ServiceResult.FailureResult("Current password is incorrect");
                }

                // Hash mật khẩu mới using PBKDF2
                var newPasswordHash = _passwordHasher.Hash(newPassword);

                // Cập nhật mật khẩu
                user.PasswordHash = Encoding.UTF8.GetBytes(newPasswordHash);
                user.UpdatedAtUtc = DateTime.UtcNow;
                
                // Reset các trường liên quan đến security
                user.FailedLoginCount = 0;
                user.LockedUntilUtc = null;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return ServiceResult.SuccessResult("Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return ServiceResult.FailureResult($"Error changing password: {ex.Message}");
            }
        }
    }
}