namespace Survey.Services
{
    public interface ITokenGenerator
    {
        string GenerateSecureToken();
    }
}