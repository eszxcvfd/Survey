using Survey.DTOs;

namespace Survey.Services
{
    public interface ISurveyCollaboratorService
    {
        Task<ManageCollaboratorsViewModel?> GetCollaboratorsForSurveyAsync(Guid surveyId, Guid currentUserId);
        Task<ServiceResult> AddCollaboratorAsync(Guid surveyId, string userEmail, string role, Guid addedByUserId);
        Task<ServiceResult> RemoveCollaboratorAsync(Guid surveyId, Guid userIdToRemove, Guid removedByUserId);
        Task<ServiceResult> UpdateRoleAsync(Guid surveyId, Guid userId, string newRole, Guid updatedByUserId);
        Task<bool> HasAccessAsync(Guid surveyId, Guid userId);
        Task<bool> IsOwnerAsync(Guid surveyId, Guid userId);
        Task<string?> GetUserRoleAsync(Guid surveyId, Guid userId);
    }
}