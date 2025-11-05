namespace Survey.Services
{
    public interface IQrCodeService
    {
        Task<string> GenerateAndSaveAsync(string url);
    }
}