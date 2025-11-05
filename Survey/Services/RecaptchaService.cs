using System.Text.Json;

namespace Survey.Services
{
    public class RecaptchaService : ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<RecaptchaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateAsync(string? captchaResponse)
        {
            _logger.LogInformation("=== CAPTCHA Validation Start ===");
            _logger.LogInformation("CaptchaResponse received: {Response}", captchaResponse ?? "NULL");
            
            if (string.IsNullOrWhiteSpace(captchaResponse))
            {
                _logger.LogWarning("CAPTCHA response is null or empty");
                return false;
            }

            var secretKey = _configuration["Recaptcha:SecretKey"];
            _logger.LogInformation("SecretKey configured: {HasKey}", !string.IsNullOrWhiteSpace(secretKey));
            
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogWarning("Recaptcha SecretKey is not configured");
                return false;
            }

            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", secretKey),
                    new KeyValuePair<string, string>("response", captchaResponse)
                });

                _logger.LogInformation("Sending request to Google reCAPTCHA API...");
                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                var jsonString = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Google API Response: {Response}", jsonString);
                
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                _logger.LogInformation("CAPTCHA validation result: {Success}", result?.Success ?? false);
                
                if (result?.ErrorCodes != null && result.ErrorCodes.Length > 0)
                {
                    _logger.LogWarning("CAPTCHA validation errors: {Errors}", string.Join(", ", result.ErrorCodes));
                }

                return result?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reCAPTCHA");
                return false;
            }
        }

        private class RecaptchaResponse
        {
            public bool Success { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("error-codes")]
            public string[] ErrorCodes { get; set; } = Array.Empty<string>();
            
            [System.Text.Json.Serialization.JsonPropertyName("challenge_ts")]
            public string? ChallengeTs { get; set; }
            
            public string? Hostname { get; set; }
        }
    }
}