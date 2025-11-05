using System.Security.Cryptography;

namespace Survey.Services
{
    public class TokenGenerator : ITokenGenerator
    {
        public string GenerateSecureToken()
        {
            // T?o token 32 bytes (256 bits) an toàn
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            // Convert sang Base64 URL-safe string
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}