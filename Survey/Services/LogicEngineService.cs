using Survey.Models;
using Survey.Repositories;

namespace Survey.Services
{
    /// <summary>
    /// The brain of the survey flow - applies branching logic dynamically
    /// </summary>
    public class LogicEngineService : ILogicEngineService
    {
        private readonly IBranchLogicRepository _logicRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IResponseAnswerRepository _answerRepository;
        private readonly ILogger<LogicEngineService> _logger;

        public LogicEngineService(
            IBranchLogicRepository logicRepository,
            IQuestionRepository questionRepository,
            IResponseAnswerRepository answerRepository,
            ILogger<LogicEngineService> logger)
        {
            _logicRepository = logicRepository;
            _questionRepository = questionRepository;
            _answerRepository = answerRepository;
            _logger = logger;
        }

        public async Task<Question?> GetNextQuestionAsync(Guid surveyId, Guid responseId, Guid? lastAnsweredQuestionId)
        {
            _logger.LogInformation("Getting next question for survey {SurveyId}, response {ResponseId}, last question {LastQuestionId}", 
                surveyId, responseId, lastAnsweredQuestionId);

            // CASE 1: Starting survey (no previous question)
            if (!lastAnsweredQuestionId.HasValue)
            {
                _logger.LogInformation("Starting survey - getting first question");
                return await _questionRepository.GetFirstQuestionAsync(surveyId);
            }

            // CASE 2: Get logic rules for the last answered question
            var rules = await _logicRepository.GetBySourceQuestionIdAsync(lastAnsweredQuestionId.Value);
            _logger.LogInformation("Found {Count} logic rules for question {QuestionId}", rules.Count, lastAnsweredQuestionId.Value);

            if (rules.Any())
            {
                // Get the answer from the respondent
                var answer = await _answerRepository.GetAnswerAsync(responseId, lastAnsweredQuestionId.Value);
                var answerOptions = await _answerRepository.GetAnswerOptionsAsync(responseId, lastAnsweredQuestionId.Value);

                _logger.LogInformation("Answer found: Text={Text}, OptionCount={Count}", 
                    answer?.AnswerText, answerOptions.Count);

                // CASE 3: Evaluate rules in priority order
                foreach (var rule in rules.OrderBy(r => r.PriorityOrder))
                {
                    bool conditionMet = await EvaluateConditionAsync(rule, answer, answerOptions);

                    if (conditionMet)
                    {
                        _logger.LogInformation("Logic rule {LogicId} matched - Action: {Action}", 
                            rule.LogicId, rule.TargetAction);

                        // Apply the action
                        switch (rule.TargetAction)
                        {
                            case "EndSurvey":
                                _logger.LogInformation("EndSurvey action triggered");
                                return null; // End survey

                            case "ShowQuestion":
                                if (rule.TargetQuestionId.HasValue)
                                {
                                    _logger.LogInformation("ShowQuestion action - jumping to {TargetQuestionId}", 
                                        rule.TargetQuestionId.Value);
                                    return await _questionRepository.GetByIdWithOptionsAsync(rule.TargetQuestionId.Value);
                                }
                                break;

                            case "SkipQuestion":
                                if (rule.TargetQuestionId.HasValue)
                                {
                                    _logger.LogInformation("SkipQuestion action - skipping to question after {TargetQuestionId}", 
                                        rule.TargetQuestionId.Value);
                                    return await _questionRepository.GetNextQuestionAfterAsync(surveyId, rule.TargetQuestionId.Value);
                                }
                                break;
                        }
                    }
                }
            }

            // CASE 4: No logic matched - proceed to next question in order
            _logger.LogInformation("No logic matched - getting next question in order");
            var nextQuestion = await _questionRepository.GetNextQuestionInOrderAsync(surveyId, lastAnsweredQuestionId.Value);

            if (nextQuestion == null)
            {
                _logger.LogInformation("No more questions - survey complete");
            }

            return nextQuestion;
        }

        private async Task<bool> EvaluateConditionAsync(BranchLogic rule, ResponseAnswer? answer, List<ResponseAnswerOption> answerOptions)
        {
            try
            {
                // Parse condition: "OptionId == 'guid'"
                var optionId = ExtractOptionIdFromCondition(rule.ConditionExpr);
                if (optionId == Guid.Empty)
                {
                    _logger.LogWarning("Could not parse condition: {Condition}", rule.ConditionExpr);
                    return false;
                }

                // Check if the respondent selected this option
                bool matched = answerOptions.Any(ao => ao.OptionId == optionId);

                _logger.LogInformation("Condition evaluation: Expected OptionId={Expected}, Found={Found}, Matched={Matched}", 
                    optionId, answerOptions.Count, matched);

                return matched;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating condition: {Condition}", rule.ConditionExpr);
                return false;
            }
        }

        private Guid ExtractOptionIdFromCondition(string conditionExpr)
        {
            try
            {
                // Parse "OptionId == 'guid'" to extract the guid
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