using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    public class SurveyTakerService : ISurveyTakerService
    {
        private readonly ISurveyResponseRepository _responseRepository;
        private readonly IResponseAnswerRepository _answerRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ILogger<SurveyTakerService> _logger;

        public SurveyTakerService(
            ISurveyResponseRepository responseRepository,
            IResponseAnswerRepository answerRepository,
            ISurveyRepository surveyRepository,
            ILogger<SurveyTakerService> logger)
        {
            _responseRepository = responseRepository;
            _answerRepository = answerRepository;
            _surveyRepository = surveyRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<Guid>> CreateNewResponseAsync(Guid surveyId, Guid? channelId, string? respondentIP)
        {
            _logger.LogInformation("Creating new response for survey {SurveyId}", surveyId);

            try
            {
                // Validate survey
                var survey = await _surveyRepository.GetByIdAsync(surveyId);
                if (survey == null)
                {
                    return ServiceResult<Guid>.FailureResult("Survey not found");
                }

                // Check if survey is active
                if (survey.Status != "Published")
                {
                    return ServiceResult<Guid>.FailureResult("This survey is not currently accepting responses");
                }

                // Check date constraints
                if (survey.OpenAtUtc.HasValue && DateTime.UtcNow < survey.OpenAtUtc.Value)
                {
                    return ServiceResult<Guid>.FailureResult("This survey has not opened yet");
                }

                if (survey.CloseAtUtc.HasValue && DateTime.UtcNow > survey.CloseAtUtc.Value)
                {
                    return ServiceResult<Guid>.FailureResult("This survey has closed");
                }

                // Check response quota
                if (survey.ResponseQuota.HasValue)
                {
                    var currentCount = await _surveyRepository.GetResponseCountAsync(surveyId);
                    if (currentCount >= survey.ResponseQuota.Value)
                    {
                        return ServiceResult<Guid>.FailureResult("This survey has reached its response limit");
                    }
                }

                // Create new response
                var response = new SurveyResponse
                {
                    ResponseId = Guid.NewGuid(),
                    SurveyId = surveyId,
                    ChannelId = channelId,
                    Status = "InProgress",
                    RespondentIP = respondentIP,
                    AnonToken = Guid.NewGuid().ToString(), // Security token
                    LastUpdatedAtUtc = DateTime.UtcNow,
                    IsLocked = false
                };

                await _responseRepository.AddAsync(response);

                _logger.LogInformation("Response created successfully: {ResponseId}", response.ResponseId);
                return ServiceResult<Guid>.SuccessResult(response.ResponseId, "Response created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating response for survey {SurveyId}", surveyId);
                return ServiceResult<Guid>.FailureResult($"Error creating response: {ex.Message}");
            }
        }

        public async Task<ServiceResult> SaveAnswerAsync(SubmitAnswerViewModel model)
        {
            _logger.LogInformation("Saving answer for response {ResponseId}, question {QuestionId}", 
                model.ResponseId, model.QuestionId);

            try
            {
                // Delete existing answer options (for re-answering)
                await _answerRepository.DeleteAnswerOptionsAsync(model.ResponseId, model.QuestionId);

                // Save text/numeric/date answer
                if (!string.IsNullOrEmpty(model.AnswerText) || model.NumericValue.HasValue || model.DateValue.HasValue)
                {
                    var answer = new ResponseAnswer
                    {
                        ResponseId = model.ResponseId,
                        QuestionId = model.QuestionId,
                        AnswerText = model.AnswerText,
                        NumericValue = model.NumericValue,
                        DateValue = model.DateValue
                    };

                    await _answerRepository.AddOrUpdateAnswerAsync(answer);
                }

                // Save selected options (for choice-based questions)
                if (model.SelectedOptionId.HasValue)
                {
                    // Single choice
                    var answerOption = new ResponseAnswerOption
                    {
                        ResponseId = model.ResponseId,
                        QuestionId = model.QuestionId,
                        OptionId = model.SelectedOptionId.Value,
                        AdditionalText = model.AdditionalText
                    };

                    await _answerRepository.AddAnswerOptionAsync(answerOption);
                }
                else if (model.SelectedOptionIds.Any())
                {
                    // Multiple choice
                    foreach (var optionId in model.SelectedOptionIds)
                    {
                        var answerOption = new ResponseAnswerOption
                        {
                            ResponseId = model.ResponseId,
                            QuestionId = model.QuestionId,
                            OptionId = optionId,
                            AdditionalText = model.AdditionalText
                        };

                        await _answerRepository.AddAnswerOptionAsync(answerOption);
                    }
                }

                // Update response timestamp
                var response = await _responseRepository.GetByIdAsync(model.ResponseId);
                if (response != null)
                {
                    response.LastUpdatedAtUtc = DateTime.UtcNow;
                    await _responseRepository.UpdateAsync(response);
                }

                _logger.LogInformation("Answer saved successfully");
                return ServiceResult.SuccessResult("Answer saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving answer for response {ResponseId}", model.ResponseId);
                return ServiceResult.FailureResult($"Error saving answer: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CompleteResponseAsync(Guid responseId)
        {
            _logger.LogInformation("Completing response {ResponseId}", responseId);

            try
            {
                var response = await _responseRepository.GetByIdAsync(responseId);
                if (response == null)
                {
                    return ServiceResult.FailureResult("Response not found");
                }

                response.Status = "Submitted";
                response.SubmittedAtUtc = DateTime.UtcNow;
                response.LastUpdatedAtUtc = DateTime.UtcNow;

                await _responseRepository.UpdateAsync(response);

                _logger.LogInformation("Response completed successfully: {ResponseId}", responseId);
                return ServiceResult.SuccessResult("Survey completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing response {ResponseId}", responseId);
                return ServiceResult.FailureResult($"Error completing survey: {ex.Message}");
            }
        }

        public async Task<bool> ValidateResponseAccessAsync(Guid responseId, string sessionToken)
        {
            var response = await _responseRepository.GetByIdAsync(responseId);
            return response != null && response.AnonToken == sessionToken;
        }
    }
}