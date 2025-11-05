using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    public class BranchLogicService : IBranchLogicService
    {
        private readonly IBranchLogicRepository _logicRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionOptionRepository _optionRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ILogger<BranchLogicService> _logger;

        public BranchLogicService(
            IBranchLogicRepository logicRepository,
            IQuestionRepository questionRepository,
            IQuestionOptionRepository optionRepository,
            ISurveyRepository surveyRepository,
            ISurveyCollaboratorRepository collaboratorRepository,
            ILogger<BranchLogicService> logger)
        {
            _logicRepository = logicRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _surveyRepository = surveyRepository;
            _collaboratorRepository = collaboratorRepository;
            _logger = logger;
        }

        public async Task<ManageLogicViewModel?> GetLogicForSurveyAsync(Guid surveyId, Guid currentUserId)
        {
            _logger.LogInformation("Getting branch logic for survey {SurveyId}", surveyId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(surveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    _logger.LogWarning("User {UserId} does not have permission to manage logic for survey {SurveyId}", currentUserId, surveyId);
                    return null;
                }

                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                if (survey == null)
                {
                    _logger.LogWarning("Survey {SurveyId} not found", surveyId);
                    return null;
                }

                // Get all questions with options
                var questions = await _questionRepository.GetBySurveyIdWithOptionsAsync(surveyId);
                _logger.LogInformation("Found {Count} questions for survey {SurveyId}", questions.Count, surveyId);

                // Get all branch logic rules
                var rules = await _logicRepository.GetBySurveyIdAsync(surveyId);
                _logger.LogInformation("Found {Count} logic rules for survey {SurveyId}", rules.Count, surveyId);

                var viewModel = new ManageLogicViewModel
                {
                    SurveyId = survey.SurveyId,
                    SurveyTitle = survey.Title,
                    AllQuestions = questions.Select(q => new QuestionReferenceViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionOrder = q.QuestionOrder,
                        QuestionType = q.QuestionType,
                        Options = q.QuestionOptions.Select(o => new OptionReferenceViewModel
                        {
                            OptionId = o.OptionId,
                            OptionText = o.OptionText,
                            OptionOrder = o.OptionOrder
                        }).ToList()
                    }).ToList(),
                    Rules = rules.Select(r => MapToBranchLogicRuleViewModel(r, questions)).ToList(),
                    NewRuleForm = new AddRuleViewModel { SurveyId = surveyId }
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logic for survey {SurveyId}", surveyId);
                return null;
            }
        }

        public async Task<ServiceResult> AddRuleAsync(AddRuleViewModel model, Guid currentUserId)
        {
            _logger.LogInformation("Adding branch logic rule for survey {SurveyId}", model.SurveyId);
            _logger.LogInformation("Rule details - SourceQuestionId: {SourceQuestionId}, SourceOptionId: {SourceOptionId}, TargetAction: {TargetAction}, TargetQuestionId: {TargetQuestionId}", 
                model.SourceQuestionId, model.SourceOptionId, model.TargetAction, model.TargetQuestionId);

            try
            {
                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(model.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to manage logic for this survey");
                }

                // Validate SourceQuestion exists
                var sourceQuestion = await _questionRepository.GetByIdWithOptionsAsync(model.SourceQuestionId);
                if (sourceQuestion == null)
                {
                    return ServiceResult.FailureResult("Source question not found");
                }

                // Validate SourceOption exists and belongs to SourceQuestion
                var sourceOption = sourceQuestion.QuestionOptions.FirstOrDefault(o => o.OptionId == model.SourceOptionId);
                if (sourceOption == null)
                {
                    return ServiceResult.FailureResult("Selected option not found or doesn't belong to the selected question");
                }

                // *** VALIDATION MỚI: Kiểm tra xem đã có logic rule cho cặp (Question + Option) này chưa ***
                var existingRules = await _logicRepository.GetBySurveyIdAsync(model.SurveyId);
                var duplicateRule = existingRules.FirstOrDefault(r => 
                {
                    // Extract option ID from existing rule's condition
                    var existingOptionId = ExtractOptionIdFromCondition(r.ConditionExpr);
                    return r.SourceQuestionId == model.SourceQuestionId && 
                           existingOptionId == model.SourceOptionId;
                });

                if (duplicateRule != null)
                {
                    return ServiceResult.FailureResult(
                        $"A logic rule already exists for this question and answer combination. " +
                        $"Please delete the existing rule first or select a different answer.");
                }

                // *** VALIDATION MỚI: Kiểm tra nếu câu hỏi là câu cuối cùng và action là EndSurvey ***
                var allQuestions = await _questionRepository.GetBySurveyIdAsync(model.SurveyId);
                var maxQuestionOrder = allQuestions.Max(q => q.QuestionOrder);
                var isLastQuestion = sourceQuestion.QuestionOrder == maxQuestionOrder;

                if (isLastQuestion && model.TargetAction == "EndSurvey")
                {
                    return ServiceResult.FailureResult(
                        "Cannot add 'End Survey' logic to the last question. " +
                        "The survey will automatically end after the last question.");
                }

                // Validate TargetAction
                if (string.IsNullOrEmpty(model.TargetAction))
                {
                    return ServiceResult.FailureResult("Please select an action");
                }

                // Validate based on TargetAction
                if (model.TargetAction == "ShowQuestion" || model.TargetAction == "SkipQuestion")
                {
                    if (!model.TargetQuestionId.HasValue)
                    {
                        var actionText = model.TargetAction == "ShowQuestion" ? "Show Question" : "Skip Question";
                        return ServiceResult.FailureResult($"Target question is required for '{actionText}' action");
                    }

                    // Validate target question exists
                    var targetQuestion = await _questionRepository.GetByIdAsync(model.TargetQuestionId.Value);
                    if (targetQuestion == null)
                    {
                        return ServiceResult.FailureResult("Target question not found");
                    }

                    // Validate target question belongs to same survey
                    if (targetQuestion.SurveyId != model.SurveyId)
                    {
                        return ServiceResult.FailureResult("Target question must belong to the same survey");
                    }

                    // Prevent circular logic (source question = target question)
                    if (model.SourceQuestionId == model.TargetQuestionId.Value)
                    {
                        return ServiceResult.FailureResult("Source and target questions cannot be the same");
                    }
                }
                else if (model.TargetAction == "EndSurvey")
                {
                    // Clear target question for end survey action
                    model.TargetQuestionId = null;
                }
                else
                {
                    return ServiceResult.FailureResult("Invalid action selected");
                }

                // Build condition expression
                var conditionExpr = $"OptionId == '{model.SourceOptionId}'";

                // Get next priority order
                var maxPriority = await _logicRepository.GetMaxPriorityOrderBySurveyIdAsync(model.SurveyId);

                // Create new rule
                var newRule = new BranchLogic
                {
                    LogicId = Guid.NewGuid(),
                    SurveyId = model.SurveyId,
                    SourceQuestionId = model.SourceQuestionId,
                    ConditionExpr = conditionExpr,
                    TargetAction = model.TargetAction,
                    TargetQuestionId = model.TargetQuestionId,
                    PriorityOrder = maxPriority + 1,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _logger.LogInformation("Creating logic rule: {LogicId} with condition: {Condition}, action: {Action}", 
                    newRule.LogicId, newRule.ConditionExpr, newRule.TargetAction);

                await _logicRepository.AddAsync(newRule);

                _logger.LogInformation("Branch logic rule added successfully: {LogicId}", newRule.LogicId);
                return ServiceResult.SuccessResult("Logic rule added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding branch logic rule for survey {SurveyId}", model.SurveyId);
                return ServiceResult.FailureResult($"Error adding logic rule: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteRuleAsync(Guid logicId, Guid currentUserId)
        {
            _logger.LogInformation("Deleting branch logic rule {LogicId}", logicId);

            try
            {
                var rule = await _logicRepository.GetByIdAsync(logicId);
                if (rule == null)
                {
                    return ServiceResult.FailureResult("Logic rule not found");
                }

                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(rule.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to manage logic for this survey");
                }

                await _logicRepository.DeleteAsync(logicId);

                _logger.LogInformation("Branch logic rule deleted successfully: {LogicId}", logicId);
                return ServiceResult.SuccessResult("Logic rule deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch logic rule {LogicId}", logicId);
                return ServiceResult.FailureResult($"Error deleting logic rule: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateRulePriorityAsync(Guid logicId, int newPriority, Guid currentUserId)
        {
            _logger.LogInformation("Updating priority for branch logic rule {LogicId}", logicId);

            try
            {
                var rule = await _logicRepository.GetByIdAsync(logicId);
                if (rule == null)
                {
                    return ServiceResult.FailureResult("Logic rule not found");
                }

                // Check permission
                var collaboration = await _collaboratorRepository.GetAsync(rule.SurveyId, currentUserId);
                if (collaboration == null || (collaboration.Role != "Owner" && collaboration.Role != "Editor"))
                {
                    return ServiceResult.FailureResult("You don't have permission to manage logic for this survey");
                }

                rule.PriorityOrder = newPriority;
                await _logicRepository.UpdateAsync(rule);

                return ServiceResult.SuccessResult("Priority updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating priority for branch logic rule {LogicId}", logicId);
                return ServiceResult.FailureResult($"Error updating priority: {ex.Message}");
            }
        }

        // Helper methods

        private BranchLogicRuleViewModel MapToBranchLogicRuleViewModel(BranchLogic rule, List<Question> allQuestions)
        {
            var sourceQuestion = allQuestions.FirstOrDefault(q => q.QuestionId == rule.SourceQuestionId);
            var targetQuestion = rule.TargetQuestionId.HasValue 
                ? allQuestions.FirstOrDefault(q => q.QuestionId == rule.TargetQuestionId.Value) 
                : null;

            // Parse condition to get option
            var optionId = ExtractOptionIdFromCondition(rule.ConditionExpr);
            var option = sourceQuestion?.QuestionOptions.FirstOrDefault(o => o.OptionId == optionId);

            var conditionDescription = option != null 
                ? $"Answer is '{option.OptionText}'"
                : "Unknown condition";

            // Generate action description based on action type
            string actionDescription;
            switch (rule.TargetAction)
            {
                case "ShowQuestion":
                    actionDescription = targetQuestion != null 
                        ? $"Jump to Q{targetQuestion.QuestionOrder}: {targetQuestion.QuestionText}"
                        : "Jump to unknown question";
                    break;
                case "SkipQuestion":
                    actionDescription = targetQuestion != null 
                        ? $"Skip to Q{targetQuestion.QuestionOrder}: {targetQuestion.QuestionText}"
                        : "Skip to unknown question";
                    break;
                case "EndSurvey":
                    actionDescription = "End survey";
                    break;
                default:
                    actionDescription = "Unknown action";
                    break;
            }

            var ruleDescription = $"IF Q{sourceQuestion?.QuestionOrder}: '{sourceQuestion?.QuestionText ?? "Unknown"}' → {conditionDescription} THEN {actionDescription}";

            return new BranchLogicRuleViewModel
            {
                LogicId = rule.LogicId,
                SourceQuestionId = rule.SourceQuestionId,
                SourceQuestionText = sourceQuestion?.QuestionText ?? "Unknown",
                ConditionDescription = conditionDescription,
                TargetAction = rule.TargetAction,
                TargetQuestionId = rule.TargetQuestionId,
                TargetQuestionText = targetQuestion?.QuestionText,
                RuleDescription = ruleDescription,
                PriorityOrder = rule.PriorityOrder
            };
        }

        private Guid ExtractOptionIdFromCondition(string conditionExpr)
        {
            // Parse "OptionId == 'guid'" to extract the guid
            try
            {
                var parts = conditionExpr.Split("==");
                if (parts.Length == 2)
                {
                    var guidString = parts[1].Trim().Trim('\'', '"', ' ');
                    if (Guid.TryParse(guidString, out var optionId))
                    {
                        return optionId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract option ID from condition: {Condition}", conditionExpr);
            }
            return Guid.Empty;
        }
    }
}