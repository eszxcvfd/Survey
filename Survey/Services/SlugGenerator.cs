using Survey.Repositories;

namespace Survey.Services
{
    public class SlugGenerator : ISlugGenerator
    {
        private readonly ISurveyChannelRepository _channelRepository;
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public SlugGenerator(ISurveyChannelRepository channelRepository)
        {
            _channelRepository = channelRepository;
        }

        public async Task<string> GenerateUniqueSlugAsync()
        {
            while (true)
            {
                var slug = GenerateRandomString(8);
                var existing = await _channelRepository.GetBySlugAsync(slug);
                if (existing == null)
                {
                    return slug;
                }
            }
        }

        private string GenerateRandomString(int length)
        {
            var random = new Random();
            return new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
    }
}