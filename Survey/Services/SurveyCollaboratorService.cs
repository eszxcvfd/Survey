using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;
using SurveyModel = Survey.Models.Survey;

namespace Survey.Services
{
    public class SurveyCollaboratorService : ISurveyCollaboratorService
    {
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SurveyCollaboratorService> _logger;

        // Valid roles
        private static readonly string[] ValidRoles = { "Owner", "Editor", "Viewer" };

        public SurveyCollaboratorService(
            ISurveyCollaboratorRepository collaboratorRepository,
            ISurveyRepository surveyRepository,
            IUserRepository userRepository,
            ILogger<SurveyCollaboratorService> logger)
        {
            _collaboratorRepository = collaboratorRepository;
            _surveyRepository = surveyRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<ManageCollaboratorsViewModel?> GetCollaboratorsForSurveyAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Getting collaborators for survey {SurveyId}", surveyId);

            // Get survey
            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                _logger.LogWarning("Survey not found: {SurveyId}", surveyId);
                return null;
            }

            // Check if current user is owner
            var isOwner = survey.OwnerId == currentUserId;

            // Get collaborators
            var collaborators = await _collaboratorRepository.GetBySurveyIdAsync(surveyId);

            // Map to view models
            var collaboratorViewModels = collaborators.Select(c => new CollaboratorViewModel
            {
                UserId = c.UserId,
                Email = c.User?.Email ?? "Unknown",
                FullName = c.User?.FullName ?? "Unknown User",
                AvatarUrl = c.User?.AvatarUrl,
                Role = c.Role,
                GrantedAtUtc = c.GrantedAtUtc,
                GrantedBy = c.GrantedBy
            }).ToList();

            // Get granted by names
            var granterIds = collaboratorViewModels
                .Where(c => c.GrantedBy.HasValue)
                .Select(c => c.GrantedBy!.Value)
                .Distinct()
                .ToList();

            foreach (var granterId in granterIds)
            {
                var granter = await _userRepository.GetUserByIdAsync(granterId);
                if (granter != null)
                {
                    foreach (var collab in collaboratorViewModels.Where(c => c.GrantedBy == granterId))
                    {
                        collab.GrantedByName = granter.FullName ?? granter.Email;
                    }
                }
            }

            return new ManageCollaboratorsViewModel
            {
                SurveyId = surveyId,
                SurveyTitle = survey.Title,
                SurveyStatus = survey.Status,
                IsOwner = isOwner,
                OwnerId = survey.OwnerId,
                Collaborators = collaboratorViewModels,
                AddForm = new AddCollaboratorDto { SurveyId = surveyId }
            };
        }

        public async Task<ServiceResult> AddCollaboratorAsync(Guid surveyId, string userEmail, string role, Guid addedByUserId)
        {
            _logger.LogInformation("Adding collaborator to survey {SurveyId}: Email={Email}, Role={Role}", 
                surveyId, userEmail, role);

            // Validate role
            if (!ValidRoles.Contains(role))
            {
                return ServiceResult.FailureResult($"Invalid role. Valid roles are: {string.Join(", ", ValidRoles)}");
            }

            // Only Owner can be managed through survey owner
            if (role == "Owner")
            {
                return ServiceResult.FailureResult("Cannot add another owner. Transfer ownership instead.");
            }

            // Check if current user is owner
            var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, addedByUserId);
            if (!isOwner)
            {
                _logger.LogWarning("User {UserId} attempted to add collaborator without owner permission", addedByUserId);
                return ServiceResult.FailureResult("Only the survey owner can add collaborators");
            }

            // Find user by email
            var userToAdd = await _userRepository.GetUserByEmailAsync(userEmail);
            if (userToAdd == null)
            {
                _logger.LogWarning("User not found with email: {Email}", userEmail);
                return ServiceResult.FailureResult($"No user found with email: {userEmail}");
            }

            // Check if already a collaborator
            var exists = await _collaboratorRepository.ExistsAsync(surveyId, userToAdd.UserId);
            if (exists)
            {
                _logger.LogWarning("User {UserId} is already a collaborator on survey {SurveyId}", 
                    userToAdd.UserId, surveyId);
                return ServiceResult.FailureResult("This user is already a collaborator on this survey");
            }

            // Create collaborator
            var collaborator = new SurveyCollaborator
            {
                SurveyId = surveyId,
                UserId = userToAdd.UserId,
                Role = role,
                GrantedAtUtc = DateTime.UtcNow,
                GrantedBy = addedByUserId
            };

            await _collaboratorRepository.AddAsync(collaborator);
            _logger.LogInformation("Collaborator added successfully: Survey={SurveyId}, User={UserId}, Role={Role}", 
                surveyId, userToAdd.UserId, role);

            return ServiceResult.SuccessResult($"{userToAdd.FullName ?? userEmail} has been added as {role}");
        }

        public async Task<ServiceResult> RemoveCollaboratorAsync(Guid surveyId, Guid userIdToRemove, Guid removedByUserId)
        {
            _logger.LogInformation("Removing collaborator from survey {SurveyId}: UserId={UserId}", 
                surveyId, userIdToRemove);

            // Check if current user is owner
            var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, removedByUserId);
            if (!isOwner)
            {
                _logger.LogWarning("User {UserId} attempted to remove collaborator without owner permission", removedByUserId);
                return ServiceResult.FailureResult("Only the survey owner can remove collaborators");
            }

            // Get collaborator
            var collaborator = await _collaboratorRepository.GetAsync(surveyId, userIdToRemove);
            if (collaborator == null)
            {
                return ServiceResult.FailureResult("Collaborator not found");
            }

            // Cannot remove owner
            if (collaborator.Role == "Owner")
            {
                return ServiceResult.FailureResult("Cannot remove the survey owner");
            }

            // Cannot remove yourself if you're the owner (to prevent accidental lockout)
            if (userIdToRemove == removedByUserId)
            {
                return ServiceResult.FailureResult("You cannot remove yourself as owner. Transfer ownership first.");
            }

            await _collaboratorRepository.DeleteAsync(surveyId, userIdToRemove);
            _logger.LogInformation("Collaborator removed successfully: Survey={SurveyId}, User={UserId}", 
                surveyId, userIdToRemove);

            return ServiceResult.SuccessResult("Collaborator removed successfully");
        }

        public async Task<ServiceResult> UpdateRoleAsync(Guid surveyId, Guid userId, string newRole, Guid updatedByUserId)
        {
            _logger.LogInformation("Updating collaborator role: Survey={SurveyId}, User={UserId}, NewRole={NewRole}", 
                surveyId, userId, newRole);

            // Validate role
            if (!ValidRoles.Contains(newRole))
            {
                return ServiceResult.FailureResult($"Invalid role. Valid roles are: {string.Join(", ", ValidRoles)}");
            }

            // Cannot change to Owner
            if (newRole == "Owner")
            {
                return ServiceResult.FailureResult("Use transfer ownership feature instead");
            }

            // Check if current user is owner
            var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, updatedByUserId);
            if (!isOwner)
            {
                _logger.LogWarning("User {UserId} attempted to update role without owner permission", updatedByUserId);
                return ServiceResult.FailureResult("Only the survey owner can update roles");
            }

            // Get collaborator
            var collaborator = await _collaboratorRepository.GetAsync(surveyId, userId);
            if (collaborator == null)
            {
                return ServiceResult.FailureResult("Collaborator not found");
            }

            // Cannot change owner role
            if (collaborator.Role == "Owner")
            {
                return ServiceResult.FailureResult("Cannot change the owner's role. Transfer ownership instead.");
            }

            // Update role
            collaborator.Role = newRole;
            await _collaboratorRepository.UpdateAsync(collaborator);

            _logger.LogInformation("Collaborator role updated successfully: Survey={SurveyId}, User={UserId}, NewRole={NewRole}", 
                surveyId, userId, newRole);

            return ServiceResult.SuccessResult($"Role updated to {newRole} successfully");
        }

        public async Task<bool> HasAccessAsync(Guid surveyId, Guid userId)
        {
            return await _collaboratorRepository.ExistsAsync(surveyId, userId);
        }

        public async Task<bool> IsOwnerAsync(Guid surveyId, Guid userId)
        {
            var collaborator = await _collaboratorRepository.GetAsync(surveyId, userId);
            return collaborator?.Role == "Owner";
        }

        public async Task<string?> GetUserRoleAsync(Guid surveyId, Guid userId)
        {
            var collaborator = await _collaboratorRepository.GetAsync(surveyId, userId);
            return collaborator?.Role;
        }
    }
}