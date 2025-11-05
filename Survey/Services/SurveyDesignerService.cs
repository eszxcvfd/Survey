using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    public class SurveyDesignerService : ISurveyDesignerService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionOptionRepository _optionRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ILogger<SurveyDesignerService> _logger;

        public SurveyDesignerService(
            IQuestionRepository questionRepository,
            IQuestionOptionRepository optionRepository,
            ISurveyRepository surveyRepository,
            ISurveyCollaboratorRepository collaboratorRepository,
            ILogger<SurveyDesignerService> logger)
        {
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _surveyRepository = surveyRepository;
            _collaboratorRepository = collaboratorRepository;
            _logger = logger;
        }

        public async Task<SurveyDesignerViewModel?> GetSurveyForDesignerAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Getting survey designer for survey {SurveyId}", surveyId);

            // Check permission
            var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
            if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
            {
                _logger.LogWarning("User {UserId} does not have permission to edit survey {SurveyId}", currentUserId, surveyId);
                return null;
            }

            var survey = await _surveyRepository.GetByIdAsync(surveyId);
            if (survey == null)
            {
                return null;
            }

            var questions = await _questionRepository.GetBySurveyIdWithOptionsAsync(surveyId);

            var viewModel = new SurveyDesignerViewModel
            {
                SurveyId = survey.SurveyId,
                SurveyTitle = survey.Title,
                Status = survey.Status,
                Questions = questions.Select(q => MapToQuestionViewModel(q)).ToList()
            };

            return viewModel;
        }

        public async Task<ServiceResult<QuestionViewModel>> AddQuestionAsync(Guid surveyId, string questionType, Guid currentUserId)
        {
            _logger.LogInformation("Adding question to survey {SurveyId}", surveyId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult<QuestionViewModel>.FailureResult("You don't have permission to edit this survey");
                }

                // Get next order
                var maxOrder = await _questionRepository.GetMaxOrderBySurveyIdAsync(surveyId);
                var newOrder = maxOrder + 1;

                // Create new question
                var newQuestion = new Question
                {
                    QuestionId = Guid.NewGuid(),
                    SurveyId = surveyId,
                    QuestionText = GetDefaultQuestionText(questionType),
                    QuestionType = questionType,
                    QuestionOrder = newOrder,
                    IsRequired = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                await _questionRepository.AddAsync(newQuestion);

                // Add default options for choice-based questions
                if (RequiresOptions(questionType))
                {
                    var defaultOptions = new List<QuestionOption>
                    {
                        new QuestionOption
                        {
                            OptionId = Guid.NewGuid(),
                            QuestionId = newQuestion.QuestionId,
                            OptionText = "Option 1",
                            OptionOrder = 1,
                            IsActive = true
                        },
                        new QuestionOption
                        {
                            OptionId = Guid.NewGuid(),
                            QuestionId = newQuestion.QuestionId,
                            OptionText = "Option 2",
                            OptionOrder = 2,
                            IsActive = true
                        }
                    };

                    await _optionRepository.AddRangeAsync(defaultOptions);
                    newQuestion.QuestionOptions = defaultOptions;
                }

                var viewModel = MapToQuestionViewModel(newQuestion);
                return ServiceResult<QuestionViewModel>.SuccessResult(viewModel, "Question added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding question to survey {SurveyId}", surveyId);
                return ServiceResult<QuestionViewModel>.FailureResult($"Error adding question: {ex.Message}");
            }
        }

        public async Task<ServiceResult<QuestionViewModel>> UpdateQuestionAsync(QuestionViewModel model, Guid currentUserId)
        {
            _logger.LogInformation("Updating question {QuestionId}", model.QuestionId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(model.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult<QuestionViewModel>.FailureResult("You don't have permission to edit this survey");
                }

                var question = await _questionRepository.GetByIdAsync(model.QuestionId);
                if (question == null)
                {
                    return ServiceResult<QuestionViewModel>.FailureResult("Question not found");
                }

                // Update question properties
                question.QuestionText = model.QuestionText;
                question.QuestionType = model.QuestionType;
                question.IsRequired = model.IsRequired;
                question.ValidationRule = model.ValidationRule;
                question.HelpText = model.HelpText;
                question.DefaultValue = model.DefaultValue;
                question.UpdatedAtUtc = DateTime.UtcNow;

                await _questionRepository.UpdateAsync(question);

                // Sync options if question type requires them
                if (RequiresOptions(model.QuestionType))
                {
                    await SyncOptionsAsync(model.QuestionId, model.Options);
                }
                else
                {
                    // Delete all options if question type doesn't need them
                    var existingOptions = await _optionRepository.GetByQuestionIdAsync(model.QuestionId);
                    if (existingOptions.Any())
                    {
                        await _optionRepository.DeleteRangeAsync(existingOptions.Select(o => o.OptionId).ToList());
                    }
                }

                // Reload question with options
                var updatedQuestion = await _questionRepository.GetByIdWithOptionsAsync(model.QuestionId);
                var viewModel = MapToQuestionViewModel(updatedQuestion!);

                return ServiceResult<QuestionViewModel>.SuccessResult(viewModel, "Question updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", model.QuestionId);
                return ServiceResult<QuestionViewModel>.FailureResult($"Error updating question: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteQuestionAsync(Guid questionId, Guid currentUserId)
        {
            _logger.LogInformation("Deleting question {QuestionId}", questionId);

            try
            {
                var question = await _questionRepository.GetByIdAsync(questionId);
                if (question == null)
                {
                    return ServiceResult.FailureResult("Question not found");
                }

                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(question.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to edit this survey");
                }

                // Delete question (options will be cascade deleted by database)
                await _questionRepository.DeleteAsync(questionId);

                return ServiceResult.SuccessResult("Question deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
                return ServiceResult.FailureResult($"Error deleting question: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ReorderQuestionsAsync(Guid surveyId, List<Guid> questionIds, Guid currentUserId)
        {
            _logger.LogInformation("Reordering questions for survey {SurveyId}", surveyId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to edit this survey");
                }

                var questions = await _questionRepository.GetBySurveyIdAsync(surveyId);

                for (int i = 0; i < questionIds.Count; i++)
                {
                    var question = questions.FirstOrDefault(q => q.QuestionId == questionIds[i]);
                    if (question != null)
                    {
                        question.QuestionOrder = i + 1;
                        question.UpdatedAtUtc = DateTime.UtcNow;
                        await _questionRepository.UpdateAsync(question);
                    }
                }

                return ServiceResult.SuccessResult("Questions reordered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering questions for survey {SurveyId}", surveyId);
                return ServiceResult.FailureResult($"Error reordering questions: {ex.Message}");
            }
        }

        // Private helper methods

        private async Task SyncOptionsAsync(Guid questionId, List<QuestionOptionViewModel> newOptions)
        {
            var existingOptions = await _optionRepository.GetByQuestionIdAsync(questionId);
            var existingOptionIds = existingOptions.Select(o => o.OptionId).ToHashSet();
            var newOptionIds = newOptions.Where(o => o.OptionId != Guid.Empty).Select(o => o.OptionId).ToHashSet();

            // Delete removed options
            var optionsToDelete = existingOptions.Where(o => !newOptionIds.Contains(o.OptionId)).Select(o => o.OptionId).ToList();
            if (optionsToDelete.Any())
            {
                await _optionRepository.DeleteRangeAsync(optionsToDelete);
            }

            // Add or update options
            for (int i = 0; i < newOptions.Count; i++)
            {
                var optionViewModel = newOptions[i];
                optionViewModel.OptionOrder = i + 1;

                if (optionViewModel.OptionId == Guid.Empty || !existingOptionIds.Contains(optionViewModel.OptionId))
                {
                    // Add new option
                    var newOption = new QuestionOption
                    {
                        OptionId = Guid.NewGuid(),
                        QuestionId = questionId,
                        OptionText = optionViewModel.OptionText,
                        OptionValue = optionViewModel.OptionValue,
                        OptionOrder = optionViewModel.OptionOrder,
                        IsActive = optionViewModel.IsActive
                    };
                    await _optionRepository.AddAsync(newOption);
                }
                else
                {
                    // Update existing option
                    var existingOption = existingOptions.First(o => o.OptionId == optionViewModel.OptionId);
                    existingOption.OptionText = optionViewModel.OptionText;
                    existingOption.OptionValue = optionViewModel.OptionValue;
                    existingOption.OptionOrder = optionViewModel.OptionOrder;
                    existingOption.IsActive = optionViewModel.IsActive;
                    await _optionRepository.UpdateAsync(existingOption);
                }
            }
        }

        private QuestionViewModel MapToQuestionViewModel(Question question)
        {
            return new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                SurveyId = question.SurveyId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                QuestionOrder = question.QuestionOrder,
                IsRequired = question.IsRequired,
                ValidationRule = question.ValidationRule,
                HelpText = question.HelpText,
                DefaultValue = question.DefaultValue,
                Options = question.QuestionOptions?.Select(o => new QuestionOptionViewModel
                {
                    OptionId = o.OptionId,
                    QuestionId = o.QuestionId,
                    OptionText = o.OptionText,
                    OptionValue = o.OptionValue,
                    OptionOrder = o.OptionOrder,
                    IsActive = o.IsActive
                }).ToList() ?? new List<QuestionOptionViewModel>()
            };
        }

        private string GetDefaultQuestionText(string questionType)
        {
            return questionType switch
            {
                "ShortText" => "Short answer question",
                "LongText" => "Long answer question",
                "MultipleChoice" => "Multiple choice question",
                "Checkboxes" => "Checkbox question",
                "Dropdown" => "Dropdown question",
                "RatingScale" => "Rating scale question",
                "Date" => "Date question",
                "Time" => "Time question",
                "Email" => "Email question",
                "Number" => "Number question",
                _ => "New question"
            };
        }

        private bool RequiresOptions(string questionType)
        {
            return questionType switch
            {
                "MultipleChoice" => true,
                "Checkboxes" => true,
                "Dropdown" => true,
                "RatingScale" => true,
                _ => false
            };
        }
    }
}