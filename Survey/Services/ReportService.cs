using Survey.DTOs;
using Survey.Repositories;

namespace Survey.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly ISurveyRepository _surveyRepository;
        private readonly ISurveyCollaboratorRepository _collaboratorRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IReportRepository reportRepository,
            IQuestionRepository questionRepository,
            ISurveyRepository surveyRepository,
            ISurveyCollaboratorRepository collaboratorRepository,
            ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository;
            _questionRepository = questionRepository;
            _surveyRepository = surveyRepository;
            _collaboratorRepository = collaboratorRepository;
            _logger = logger;
        }

        public async Task<ReportViewModel> GetAggregatedReportAsync(Guid surveyId, FilterViewModel filters, Guid currentUserId)
        {
            _logger.LogInformation("Getting aggregated report for survey {SurveyId}", surveyId);

            // ✅ Kiểm tra quyền truy cập - Owner, Editor, Viewer đều có thể xem report
            var hasAccess = await CheckAccessAsync(surveyId, currentUserId);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You don't have access to this survey");
            }

            // Get survey info
            var surveyInfo = await _surveyRepository.GetByIdAsync(surveyId);
            if (surveyInfo == null)
            {
                throw new ArgumentException("Survey not found");
            }

            // Get aggregated stats
            var rawStats = await _reportRepository.GetAggregatedStatsAsync(surveyId, filters);
            var questions = await _questionRepository.GetBySurveyIdWithOptionsAsync(surveyId);

            // Get response counts
            var totalResponses = await _reportRepository.GetTotalResponsesAsync(surveyId, filters);
            var completedResponses = await _reportRepository.GetCompletedResponsesAsync(surveyId, filters);

            // ✅ Tính Answer Rate
            var totalQuestions = questions.Count;
            var totalAnsweredQuestions = rawStats.Select(s => s.QuestionId).Distinct().Count();
            var answerRate = totalQuestions > 0 && totalResponses > 0
                ? Math.Round((double)totalAnsweredQuestions / totalQuestions * 100, 1)
                : 0;

            // Build report
            var report = new ReportViewModel
            {
                SurveyId = surveyId,
                SurveyTitle = surveyInfo.Title,
                Filters = filters,
                TotalResponses = totalResponses,
                CompletedResponses = completedResponses,
                TotalQuestions = totalQuestions,
                TotalAnsweredQuestions = totalAnsweredQuestions,
                AnswerRate = answerRate,
                QuestionStats = new List<QuestionStatViewModel>()
            };

            // Process each question
            foreach (var question in questions.OrderBy(q => q.QuestionOrder))
            {
                var stat = new QuestionStatViewModel
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    QuestionOrder = question.QuestionOrder,
                    DataPoints = new List<ChartDataPoint>()
                };

                // Get stats for this question
                var questionStats = rawStats.Where(s => s.QuestionId == question.QuestionId).ToList();
                stat.TotalAnswers = questionStats.Sum(s => s.Value);

                // ✅ Generate data points (for both choice-based and text-based)
                if (questionStats.Any())
                {
                    var colors = new[] { "#6750A4", "#958DA5", "#B8B1C8", "#D0C9D6", "#E8E5EC" };
                    int colorIndex = 0;

                    foreach (var item in questionStats)
                    {
                        var percentage = stat.TotalAnswers > 0 
                            ? Math.Round((double)item.Value / stat.TotalAnswers * 100, 1) 
                            : 0;

                        stat.DataPoints.Add(new ChartDataPoint
                        {
                            Label = item.Label,
                            Value = item.Value,
                            Percentage = percentage,
                            Color = colors[colorIndex % colors.Length]
                        });

                        colorIndex++;
                    }
                }

                report.QuestionStats.Add(stat);
            }

            return report;
        }

        public async Task<List<RawResponseViewModel>> GetRawResponsesAsync(Guid surveyId, FilterViewModel filters, Guid currentUserId)
        {
            _logger.LogInformation("Getting raw responses for survey {SurveyId}", surveyId);

            // ✅ Kiểm tra quyền: CHỈ Owner và Editor mới export được, Viewer KHÔNG được
            var role = await _collaboratorRepository.GetRoleAsync(surveyId, currentUserId);
            
            // Check if owner
            var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, currentUserId);
            
            if (!isOwner && (role == null || role == "Viewer"))
            {
                throw new UnauthorizedAccessException("You don't have permission to export data. Only Owner and Editor can export.");
            }

            return await _reportRepository.GetRawResponsesAsync(surveyId, filters);
        }

        // ✅ Helper method để kiểm tra quyền truy cập (Owner, Editor, Viewer)
        private async Task<bool> CheckAccessAsync(Guid surveyId, Guid currentUserId)
        {
            // Kiểm tra owner
            var isOwner = await _surveyRepository.IsOwnerAsync(surveyId, currentUserId);
            if (isOwner) return true;

            // Kiểm tra collaborator (Editor hoặc Viewer)
            return await _collaboratorRepository.ExistsAsync(surveyId, currentUserId);
        }
    }
}