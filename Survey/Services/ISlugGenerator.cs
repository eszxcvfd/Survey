namespace Survey.Services
{
    public interface ISlugGenerator
    {
        Task<string> GenerateUniqueSlugAsync();
    }
}