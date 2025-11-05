namespace Survey.DTOs
{
    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool RequiresCaptcha { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public Models.User? User { get; set; }

        public static LoginResult Success(Models.User user)
        {
            return new LoginResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public static LoginResult Failure(string errorMessage, bool requiresCaptcha = false, DateTime? lockoutEndUtc = null)
        {
            return new LoginResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                RequiresCaptcha = requiresCaptcha,
                LockoutEndUtc = lockoutEndUtc
            };
        }
    }
}