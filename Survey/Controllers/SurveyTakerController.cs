using Microsoft.AspNetCore.Mvc;
using Survey.DTOs;
using Survey.Models;
using Survey.Repositories;
using Survey.Services;

namespace Survey.Controllers
{
    public class SurveyTakerController : Controller
    {
        private readonly ISurveyTakerService _takerService;
        private readonly ILogicEngineService _logicEngine;
        private readonly ISurveyChannelRepository _channelRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyResponseRepository _responseRepository;
        private readonly ILogger<SurveyTakerController> _logger;

        private const string SESSION_KEY_RESPONSE_ID = "CurrentResponseId";
        private const string SESSION_KEY_SURVEY_ID = "CurrentSurveyId";

        public SurveyTakerController(
            ISurveyTakerService takerService,
            ILogicEngineService logicEngine,
            ISurveyChannelRepository channelRepository,
            ISurveyRepository surveyRepository,
            ISurveyResponseRepository responseRepository,
            ILogger<SurveyTakerController> logger)
        {
            _takerService = takerService;
            _logicEngine = logicEngine;
            _channelRepository = channelRepository;
            _surveyRepository = surveyRepository;
            _responseRepository = responseRepository;
            _logger = logger;
        }

        /// <summary>
        /// Start survey from a public link (e.g., /take/kH8dFp)
        /// </summary>
        [HttpGet("/take/{slug}")]
        public async Task<IActionResult> Start(string slug)
        {
            _logger.LogInformation("=== Starting survey with slug: {Slug} ===", slug);

            try
            {
                // 1. Find channel by slug
                var channel = await _channelRepository.GetBySlugAsync(slug);
                if (channel == null || !channel.IsActive)
                {
                    ViewBag.ErrorMessage = "This survey link is not valid or has been deactivated.";
                    return View("SurveyError");
                }

                var survey = channel.Survey;
                if (survey == null)
                {
                    ViewBag.ErrorMessage = "Survey not found.";
                    return View("SurveyError");
                }

                // 2. Create new response
                var clientIP = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _takerService.CreateNewResponseAsync(survey.SurveyId, channel.ChannelId, clientIP);

                if (!result.Success)
                {
                    ViewBag.ErrorMessage = result.Message;
                    return View("SurveyError");
                }

                var responseId = result.Data;

                // 3. Store IDs in session
                HttpContext.Session.SetString(SESSION_KEY_RESPONSE_ID, responseId.ToString());
                HttpContext.Session.SetString(SESSION_KEY_SURVEY_ID, survey.SurveyId.ToString());

                // 4. Get first question
                var firstQuestion = await _logicEngine.GetNextQuestionAsync(survey.SurveyId, responseId, null);

                if (firstQuestion == null)
                {
                    ViewBag.ErrorMessage = "This survey has no questions.";
                    return View("SurveyError");
                }

                // 5. Calculate progress
                var totalQuestions = await _surveyRepository.GetQuestionCountAsync(survey.SurveyId);

                // 6. Create view model
                var viewModel = new TakeSurveyViewModel
                {
                    ResponseId = responseId,
                    SurveyId = survey.SurveyId,
                    SurveyTitle = survey.Title,
                    SurveyDescription = survey.Description,
                    CurrentQuestion = MapToQuestionViewModel(firstQuestion),
                    TotalQuestions = totalQuestions,
                    CurrentQuestionNumber = 1,
                    IsLastQuestion = totalQuestions == 1,
                    ProgressPercentage = totalQuestions > 0 ? (100 / totalQuestions) : 100
                };

                return View("DisplayQuestion", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting survey with slug: {Slug}", slug);
                ViewBag.ErrorMessage = "An error occurred while loading the survey.";
                return View("SurveyError");
            }
        }

        /// <summary>
        /// Submit answer and get next question
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer(SubmitAnswerViewModel model)
        {
            _logger.LogInformation("=== Submitting answer for response {ResponseId}, question {QuestionId} ===", 
                model.ResponseId, model.QuestionId);

            try
            {
                // 1. Validate session
                var sessionResponseId = HttpContext.Session.GetString(SESSION_KEY_RESPONSE_ID);
                var sessionSurveyId = HttpContext.Session.GetString(SESSION_KEY_SURVEY_ID);

                if (string.IsNullOrEmpty(sessionResponseId) || 
                    !Guid.TryParse(sessionResponseId, out var responseId) || 
                    responseId != model.ResponseId)
                {
                    ViewBag.ErrorMessage = "Invalid session. Please start the survey again.";
                    return View("SurveyError");
                }

                if (string.IsNullOrEmpty(sessionSurveyId) || !Guid.TryParse(sessionSurveyId, out var surveyId))
                {
                    ViewBag.ErrorMessage = "Survey session expired.";
                    return View("SurveyError");
                }

                // 2. Save answer
                var saveResult = await _takerService.SaveAnswerAsync(model);
                if (!saveResult.Success)
                {
                    ViewBag.ErrorMessage = saveResult.Message;
                    return View("SurveyError");
                }

                // 3. Apply logic engine to get next question
                var nextQuestion = await _logicEngine.GetNextQuestionAsync(surveyId, model.ResponseId, model.QuestionId);

                // 4. Check if survey is complete
                if (nextQuestion == null)
                {
                    // Complete the response
                    await _takerService.CompleteResponseAsync(model.ResponseId);

                    // Get survey info
                    var survey = await _surveyRepository.GetByIdAsync(surveyId);

                    // Clear session
                    HttpContext.Session.Remove(SESSION_KEY_RESPONSE_ID);
                    HttpContext.Session.Remove(SESSION_KEY_SURVEY_ID);

                    // Show thank you page
                    var completedViewModel = new SurveyCompletedViewModel
                    {
                        ResponseId = model.ResponseId,
                        SurveyTitle = survey?.Title ?? "Survey",
                        CompletedAt = DateTime.UtcNow
                    };

                    return View("ThankYou", completedViewModel);
                }

                // 5. Get survey for view
                var currentSurvey = await _surveyRepository.GetByIdAsync(surveyId);
                var totalQuestions = await _surveyRepository.GetQuestionCountAsync(surveyId);

                // 6. Calculate progress (get answered questions count)
                var response = await _responseRepository.GetByIdWithAnswersAsync(model.ResponseId);
                var answeredCount = response?.ResponseAnswers.Count ?? 0;
                var currentQuestionNumber = answeredCount + 1;

                // 7. Show next question
                var viewModel = new TakeSurveyViewModel
                {
                    ResponseId = model.ResponseId,
                    SurveyId = surveyId,
                    SurveyTitle = currentSurvey?.Title ?? "Survey",
                    SurveyDescription = currentSurvey?.Description,
                    CurrentQuestion = MapToQuestionViewModel(nextQuestion),
                    TotalQuestions = totalQuestions,
                    CurrentQuestionNumber = currentQuestionNumber,
                    IsLastQuestion = false, // Logic engine will return null if it's the last
                    ProgressPercentage = totalQuestions > 0 ? (currentQuestionNumber * 100) / totalQuestions : 100
                };

                return View("DisplayQuestion", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer for response {ResponseId}", model.ResponseId);
                ViewBag.ErrorMessage = "An error occurred while processing your answer.";
                return View("SurveyError");
            }
        }

        // Helper method
        private QuestionViewModel MapToQuestionViewModel(Models.Question question)
        {
            return new QuestionViewModel
            {
                QuestionId = question.QuestionId,
                SurveyId = question.SurveyId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                QuestionOrder = question.QuestionOrder,
                IsRequired = question.IsRequired,
                HelpText = question.HelpText,
                Options = question.QuestionOptions?.Select(o => new QuestionOptionViewModel
                {
                    OptionId = o.OptionId,
                    QuestionId = o.QuestionId,
                    OptionText = o.OptionText,
                    OptionOrder = o.OptionOrder,
                    IsActive = o.IsActive
                }).ToList() ?? new List<QuestionOptionViewModel>()
            };
        }
    }
}