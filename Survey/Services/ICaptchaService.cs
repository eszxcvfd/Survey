namespace Survey.Services
{
    public interface ICaptchaService
    {
        Task<bool> ValidateAsync(string? captchaResponse);
    }
}