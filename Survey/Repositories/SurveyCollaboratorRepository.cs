using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class SurveyCollaboratorRepository : ISurveyCollaboratorRepository
    {
        private readonly SurveyDbContext _context;

        public SurveyCollaboratorRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<SurveyCollaborator?> GetAsync(Guid surveyId, Guid userId)
        {
            return await _context.SurveyCollaborators
                .Include(sc => sc.User)
                .Include(sc => sc.Survey)
                .FirstOrDefaultAsync(sc => sc.SurveyId == surveyId && sc.UserId == userId);
        }

        public async Task<List<SurveyCollaborator>> GetBySurveyIdAsync(Guid surveyId)
        {
            return await _context.SurveyCollaborators
                .Include(sc => sc.User)
                .Where(sc => sc.SurveyId == surveyId)
                .OrderByDescending(sc => sc.Role == "Owner")
                .ThenBy(sc => sc.GrantedAtUtc)
                .ToListAsync();
        }

        public async Task<List<SurveyCollaborator>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SurveyCollaborators
                .Include(sc => sc.Survey)
                .Where(sc => sc.UserId == userId)
                .OrderByDescending(sc => sc.GrantedAtUtc)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid surveyId, Guid userId)
        {
            return await _context.SurveyCollaborators
                .AnyAsync(sc => sc.SurveyId == surveyId && sc.UserId == userId);
        }

        public async Task<string?> GetRoleAsync(Guid surveyId, Guid userId)
        {
            var collaborator = await _context.SurveyCollaborators
                .FirstOrDefaultAsync(sc => sc.SurveyId == surveyId && sc.UserId == userId);
            return collaborator?.Role;
        }

        public async Task AddAsync(SurveyCollaborator collaborator)
        {
            await _context.SurveyCollaborators.AddAsync(collaborator);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SurveyCollaborator collaborator)
        {
            _context.SurveyCollaborators.Update(collaborator);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid surveyId, Guid userId)
        {
            var collaborator = await GetAsync(surveyId, userId);
            if (collaborator != null)
            {
                _context.SurveyCollaborators.Remove(collaborator);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetCollaboratorCountAsync(Guid surveyId)
        {
            return await _context.SurveyCollaborators
                .CountAsync(sc => sc.SurveyId == surveyId);
        }
    }
}