using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;
using SurveyModel = Survey.Models.Survey;

namespace Survey.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ILogger<SurveyService> _logger;

        public SurveyService(
            ISurveyRepository surveyRepository,
            ISurveyCollaboratorRepository collaboratorRepository,
            ILogger<SurveyService> logger)
        {
            _surveyRepository = surveyRepository;
            _collaboratorRepository = collaboratorRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<SurveyModel>> CreateSurveyAsync(CreateSurveyDto model, Guid ownerId)
        {
            _logger.LogInformation("=== START Creating new survey for user {UserId} ===", ownerId);
            _logger.LogInformation("Survey Title: {Title}", model.Title);

            try
            {
                // Create survey
                var survey = new SurveyModel
                {
                    SurveyId = Guid.NewGuid(),
                    OwnerId = ownerId,
                    Title = model.Title,
                    Description = model.Description,
                    IsAnonymous = model.IsAnonymous,
                    DefaultLanguage = model.DefaultLanguage ?? "en",
                    Status = "Draft",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _logger.LogInformation("Survey object created with ID: {SurveyId}", survey.SurveyId);

                // Save survey first
                await _surveyRepository.AddAsync(survey);
                _logger.LogInformation("Survey saved to database successfully");

                // IMPORTANT: Add owner as collaborator with "Owner" role
                var ownerCollaborator = new SurveyCollaborator
                {
                    SurveyId = survey.SurveyId,
                    UserId = ownerId,
                    Role = "Owner",
                    GrantedBy = ownerId,
                    GrantedAtUtc = DateTime.UtcNow
                };

                _logger.LogInformation("Creating owner collaborator record");
                await _collaboratorRepository.AddAsync(ownerCollaborator);
                _logger.LogInformation("Owner collaborator saved successfully");

                _logger.LogInformation("=== COMPLETED Survey created successfully: {SurveyId} ===", survey.SurveyId);
                return ServiceResult<SurveyModel>.SuccessResult(survey, "Survey created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== ERROR creating survey for user {UserId}. Exception: {Message} ===", ownerId, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return ServiceResult<SurveyModel>.FailureResult($"Error creating survey: {ex.Message}");
            }
        }

        public async Task<MySurveysViewModel> GetMySurveysAsync(Guid currentUserId, string filter = "all")
        {
            _logger.LogInformation("Getting surveys for user {UserId} with filter {Filter}", currentUserId, filter);

            var viewModel = new MySurveysViewModel
            {
                CurrentFilter = filter
            };

            try
            {
                // Get all collaborations for current user
                var collaborations = await _collaboratorRepository.GetByUserIdAsync(currentUserId);
                
                _logger.LogInformation("Found {Count} collaborations for user", collaborations.Count);

                // Get survey IDs
                var surveyIds = collaborations.Select(c => c.SurveyId).ToList();

                // Get surveys with details
                var surveys = new List<SurveyModel>();
                foreach (var surveyId in surveyIds)
                {
                    var survey = await _surveyRepository.GetByIdAsync(surveyId);
                    if (survey != null)
                    {
                        surveys.Add(survey);
                    }
                }

                _logger.LogInformation("Retrieved {Count} surveys", surveys.Count);

                // Apply filter
                var filteredSurveys = filter switch
                {
                    "owned" => surveys.Where(s => s.OwnerId == currentUserId).ToList(),
                    "shared" => surveys.Where(s => s.OwnerId != currentUserId).ToList(),
                    "draft" => surveys.Where(s => s.Status == "Draft").ToList(),
                    "published" => surveys.Where(s => s.Status == "Published").ToList(),
                    _ => surveys
                };

                // Map to view models
                var surveyViewModels = new List<SurveyListItemViewModel>();
                foreach (var survey in filteredSurveys.OrderByDescending(s => s.UpdatedAtUtc))
                {
                    var collaboration = collaborations.First(c => c.SurveyId == survey.SurveyId);
                    var questionCount = await _surveyRepository.GetQuestionCountAsync(survey.SurveyId);
                    var responseCount = await _surveyRepository.GetResponseCountAsync(survey.SurveyId);

                    surveyViewModels.Add(new SurveyListItemViewModel
                    {
                        SurveyId = survey.SurveyId,
                        Title = survey.Title,
                        Description = survey.Description,
                        Status = survey.Status,
                        MyRole = collaboration.Role,
                        IsAnonymous = survey.IsAnonymous,
                        CreatedAtUtc = survey.CreatedAtUtc,
                        UpdatedAtUtc = survey.UpdatedAtUtc,
                        QuestionCount = questionCount,
                        ResponseCount = responseCount,
                        OpenAtUtc = survey.OpenAtUtc,
                        CloseAtUtc = survey.CloseAtUtc
                    });
                }

                viewModel.Surveys = surveyViewModels;
                viewModel.TotalSurveys = surveys.Count;
                viewModel.OwnedSurveys = surveys.Count(s => s.OwnerId == currentUserId);
                viewModel.SharedSurveys = surveys.Count(s => s.OwnerId != currentUserId);
                viewModel.DraftSurveys = surveys.Count(s => s.Status == "Draft");
                viewModel.PublishedSurveys = surveys.Count(s => s.Status == "Published");

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surveys for user {UserId}", currentUserId);
                return viewModel;
            }
        }

        public async Task<SurveyModel?> GetSurveyByIdAsync(Guid surveyId)
        {
            return await _surveyRepository.GetByIdWithDetailsAsync(surveyId);
        }

        public async Task<ServiceResult> UpdateSurveyAsync(Guid surveyId, CreateSurveyDto model, Guid currentUserId)
        {
            _logger.LogInformation("Updating survey {SurveyId}", surveyId);

            try
            {
                // Check if user is owner
                var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, currentUserId);
                if (!isOwner)
                {
                    return ServiceResult.FailureResult("Only the survey owner can update survey settings");
                }

                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                if (survey == null)
                {
                    return ServiceResult.FailureResult("Survey not found");
                }

                survey.Title = model.Title;
                survey.Description = model.Description;
                survey.IsAnonymous = model.IsAnonymous;
                survey.DefaultLanguage = model.DefaultLanguage ?? "en";
                survey.UpdatedAtUtc = DateTime.UtcNow;

                await _surveyRepository.UpdateAsync(survey);

                return ServiceResult.SuccessResult("Survey updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey {SurveyId}", surveyId);
                return ServiceResult.FailureResult($"Error updating survey: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteSurveyAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Deleting survey {SurveyId}", surveyId);

            try
            {
                // Check if user is owner
                var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, currentUserId);
                if (!isOwner)
                {
                    return ServiceResult.FailureResult("Only the survey owner can delete the survey");
                }

                await _surveyRepository.DeleteAsync(surveyId);

                return ServiceResult.SuccessResult("Survey deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting survey {SurveyId}", surveyId);
                return ServiceResult.FailureResult($"Error deleting survey: {ex.Message}");
            }
        }

        public async Task<bool> HasAccessAsync(Guid surveyId, Guid userId)
        {
            return await _collaboratorRepository.ExistsAsync(surveyId, userId);
        }

        // NEW: Get Survey Settings
        public async Task<SurveySettingsViewModel?> GetSurveySettingsAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Getting survey settings for {SurveyId}", surveyId);

            try
            {
                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                if (survey == null)
                {
                    return null;
                }

                // ? FIX: Only Owner can access settings
                var isOwner = survey.OwnerId == currentUserId;
                if (!isOwner)
                {
                    // Check collaborator table
                    var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
                    if (collaboration == null || collaboration.Role != "Owner")
                    {
                        _logger.LogWarning("User {UserId} does not have permission to access settings for survey {SurveyId}", currentUserId, surveyId);
                        return null;
                    }
                }

                // Get counts
                var questionCount = await _surveyRepository.GetQuestionCountAsync(surveyId);
                var responseCount = await _surveyRepository.GetResponseCountAsync(surveyId);

                // Map to view model
                var viewModel = new SurveySettingsViewModel
                {
                    SurveyId = survey.SurveyId,
                    Title = survey.Title,
                    Description = survey.Description,
                    DefaultLanguage = survey.DefaultLanguage,
                    Status = survey.Status,
                    IsAnonymous = survey.IsAnonymous,
                    OpenAtUtc = survey.OpenAtUtc,
                    CloseAtUtc = survey.CloseAtUtc,
                    ResponseQuota = survey.ResponseQuota,
                    QuotaBehavior = survey.QuotaBehavior,
                    CreatedAtUtc = survey.CreatedAtUtc,
                    UpdatedAtUtc = survey.UpdatedAtUtc,
                    QuestionCount = questionCount,
                    ResponseCount = responseCount
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting survey settings for {SurveyId}", surveyId);
                return null;
            }
        }

        // NEW: Update Survey Settings
        public async Task<ServiceResult> UpdateSurveySettingsAsync(SurveySettingsViewModel model, Guid currentUserId)
        {
            _logger.LogInformation("Updating survey settings for {SurveyId}", model.SurveyId);

            try
            {
                var survey = await _surveyRepository.GetByIdAsync(model.SurveyId);
                if (survey == null)
                {
                    return ServiceResult.FailureResult("Survey not found");
                }

                // ? FIX: Only Owner can update settings
                var isOwner = survey.OwnerId == currentUserId;
                if (!isOwner)
                {
                    // Check collaborator table
                    var collaboration = await _collaboratorRepository.GetAsync(model.SurveyId, currentUserId);
                    if (collaboration == null || collaboration.Role != "Owner")
                    {
                        return ServiceResult.FailureResult("Only survey owners can update settings");
                    }
                }

                // Business rule validation: CloseAtUtc must be after OpenAtUtc
                if (model.CloseAtUtc.HasValue && model.OpenAtUtc.HasValue && model.CloseAtUtc.Value <= model.OpenAtUtc.Value)
                {
                    return ServiceResult.FailureResult("Closing date must be after opening date");
                }

                // Check if survey has responses and status is changing to Draft
                if (survey.Status != "Draft" && model.Status == "Draft")
                {
                    var responseCount = await _surveyRepository.GetResponseCountAsync(model.SurveyId);
                    if (responseCount > 0)
                    {
                        return ServiceResult.FailureResult("Cannot change status to Draft for a survey that already has responses");
                    }
                }

                // Update survey properties
                survey.Title = model.Title;
                survey.Description = model.Description;
                survey.DefaultLanguage = model.DefaultLanguage;
                survey.Status = model.Status;
                survey.IsAnonymous = model.IsAnonymous;
                survey.OpenAtUtc = model.OpenAtUtc;
                survey.CloseAtUtc = model.CloseAtUtc;
                survey.ResponseQuota = model.ResponseQuota;
                survey.QuotaBehavior = model.QuotaBehavior;
                survey.UpdatedAtUtc = DateTime.UtcNow;

                await _surveyRepository.UpdateAsync(survey);

                _logger.LogInformation("Survey settings updated successfully for {SurveyId}", model.SurveyId);
                return ServiceResult.SuccessResult("Survey settings updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey settings for {SurveyId}", model.SurveyId);
                return ServiceResult.FailureResult($"Error updating survey settings: {ex.Message}");
            }
        }
    }
}