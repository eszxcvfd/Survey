using Survey.DTOs;
using Survey.Models;

namespace Survey.Services
{
    public interface IUserService
    {
        Task<ServiceResult<User>> RegisterUserAsync(RegisterDto model);
        Task<ServiceResult<User>> AuthenticateAsync(LoginDto model);
        Task<User?> GetUserByEmailAsync(string email);
    }
}