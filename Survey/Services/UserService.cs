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

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ServiceResult<User>> RegisterUserAsync(RegisterDto model)
        {
            // 1. Kiểm tra email đã tồn tại
            var existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return ServiceResult<User>.FailureResult("Email already exists");
            }

            // 2. Hash password
            var passwordHash = HashPassword(model.Password);

            // 3. Tạo User entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = model.Email,
                PasswordHash = passwordHash,
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
            // 1. Tìm user theo email
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                return ServiceResult<User>.FailureResult("Invalid email or password");
            }

            // 2. Kiểm tra tài khoản có bị khóa không
            if (!user.IsActive)
            {
                return ServiceResult<User>.FailureResult("Account is inactive");
            }

            if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow)
            {
                return ServiceResult<User>.FailureResult($"Account is locked until {user.LockedUntilUtc.Value:g}");
            }

            // 3. Verify password
            var passwordHash = HashPassword(model.Password);
            if (!VerifyPassword(user.PasswordHash, passwordHash))
            {
                // Tăng failed login count
                user.FailedLoginCount++;
                
                // Khóa tài khoản nếu login fail quá 5 lần
                if (user.FailedLoginCount >= 5)
                {
                    user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15);
                }
                
                await _userRepository.UpdateAsync(user);
                return ServiceResult<User>.FailureResult("Invalid email or password");
            }

            // 4. Reset failed login count khi đăng nhập thành công
            if (user.FailedLoginCount > 0)
            {
                user.FailedLoginCount = 0;
                user.LockedUntilUtc = null;
                await _userRepository.UpdateAsync(user);
            }

            return ServiceResult<User>.SuccessResult(user, "Login successful");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetUserByEmailAsync(email);
        }

        // Hash password sử dụng SHA256
        private byte[] HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        // Verify password
        private bool VerifyPassword(byte[] storedHash, byte[] providedHash)
        {
            if (storedHash.Length != providedHash.Length)
                return false;

            for (int i = 0; i < storedHash.Length; i++)
            {
                if (storedHash[i] != providedHash[i])
                    return false;
            }

            return true;
        }
    }
}