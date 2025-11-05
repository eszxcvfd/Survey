using Survey.DTOs;
using Survey.Models;

namespace Survey.Services
{
    public interface IUserService
    {
        Task<ServiceResult<User>> RegisterUserAsync(RegisterDto model);
        Task<ServiceResult<User>> AuthenticateAsync(LoginDto model);
        Task<LoginResult> ValidateUserAsync(string email, string password, string? captchaResponse);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<ServiceResult> GeneratePasswordResetTokenAsync(string email, string baseUrl);
        Task<ServiceResult> ResetPasswordAsync(string token, string newPassword);
        Task<ServiceResult> UpdateProfileAsync(Guid userId, EditProfileDto model);
        Task<ServiceResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }
}