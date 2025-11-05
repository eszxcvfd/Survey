using Dapper;
using Microsoft.Data.SqlClient;
using Survey.DTOs;
using System.Data;

namespace Survey.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ReportRepository> _logger;

        public ReportRepository(IConfiguration configuration, ILogger<ReportRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("Connection string not found");
            _logger = logger;
        }

        public async Task<List<AggregatedResultDto>> GetAggregatedStatsAsync(Guid surveyId, FilterViewModel filters)
        {
            _logger.LogInformation("Getting aggregated stats for survey {SurveyId}", surveyId);

            using var connection = new SqlConnection(_connectionString);
            
            // ✅ QUERY 1: Stats for choice-based questions (Multiple Choice, Checkboxes, Dropdown, Rating)
            var choiceStatsQuery = @"
                SELECT 
                    rao.QuestionId, 
                    rao.OptionId, 
                    qo.OptionText AS Label,
                    COUNT(DISTINCT rao.ResponseId) AS Value
                FROM 
                    ResponseAnswerOptions rao
                INNER JOIN 
                    QuestionOptions qo ON rao.OptionId = qo.OptionId
                INNER JOIN
                    SurveyResponses r ON rao.ResponseId = r.ResponseId
                WHERE 
                    r.SurveyId = @SurveyId
                    AND (@StartDate IS NULL OR r.SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR r.SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR r.ChannelId = @ChannelId)
                    AND (@Status IS NULL OR r.Status = @Status)
                GROUP BY 
                    rao.QuestionId, rao.OptionId, qo.OptionText
                ORDER BY 
                    rao.QuestionId, COUNT(DISTINCT rao.ResponseId) DESC";

            var choiceStats = await connection.QueryAsync<AggregatedResultDto>(choiceStatsQuery, new
            {
                SurveyId = surveyId,
                StartDate = filters.StartDate,
                EndDate = filters.EndDate,
                ChannelId = filters.ChannelId,
                Status = filters.Status
            });

            // ✅ QUERY 2: Stats for text-based questions (ShortText, LongText, Number, Email, Date)
            var textStatsQuery = @"
                SELECT 
                    ra.QuestionId,
                    NULL AS OptionId,
                    'Answered' AS Label,
                    COUNT(DISTINCT ra.ResponseId) AS Value
                FROM 
                    ResponseAnswers ra
                INNER JOIN
                    SurveyResponses r ON ra.ResponseId = r.ResponseId
                WHERE 
                    r.SurveyId = @SurveyId
                    AND (ra.AnswerText IS NOT NULL OR ra.NumericValue IS NOT NULL OR ra.DateValue IS NOT NULL)
                    AND (@StartDate IS NULL OR r.SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR r.SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR r.ChannelId = @ChannelId)
                    AND (@Status IS NULL OR r.Status = @Status)
                GROUP BY 
                    ra.QuestionId";

            var textStats = await connection.QueryAsync<AggregatedResultDto>(textStatsQuery, new
            {
                SurveyId = surveyId,
                StartDate = filters.StartDate,
                EndDate = filters.EndDate,
                ChannelId = filters.ChannelId,
                Status = filters.Status
            });

            // Merge results
            var allStats = choiceStats.ToList();
            allStats.AddRange(textStats);

            return allStats;
        }

        public async Task<List<RawResponseViewModel>> GetRawResponsesAsync(Guid surveyId, FilterViewModel filters)
        {
            _logger.LogInformation("Getting raw responses for survey {SurveyId}", surveyId);

            using var connection = new SqlConnection(_connectionString);
            
            // ✅ FIX: Query riêng cho text-based answers
            var textAnswersQuery = @"
                SELECT 
                    r.ResponseId,
                    r.SubmittedAtUtc AS SubmittedAt,
                    sc.ChannelType,
                    q.QuestionText,
                    q.QuestionOrder,
                    COALESCE(
                        ra.AnswerText, 
                        CAST(ra.NumericValue AS NVARCHAR), 
                        CAST(ra.DateValue AS NVARCHAR)
                    ) AS Answer
                FROM 
                    SurveyResponses r
                LEFT JOIN
                    SurveyChannels sc ON r.ChannelId = sc.ChannelId
                INNER JOIN
                    ResponseAnswers ra ON r.ResponseId = ra.ResponseId
                INNER JOIN
                    Questions q ON ra.QuestionId = q.QuestionId
                WHERE 
                    r.SurveyId = @SurveyId
                    AND r.Status = 'Submitted'
                    AND (ra.AnswerText IS NOT NULL OR ra.NumericValue IS NOT NULL OR ra.DateValue IS NOT NULL)
                    AND (@StartDate IS NULL OR r.SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR r.SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR r.ChannelId = @ChannelId)";

            // ✅ FIX: Query riêng cho option-based answers
            var optionAnswersQuery = @"
                SELECT 
                    r.ResponseId,
                    r.SubmittedAtUtc AS SubmittedAt,
                    sc.ChannelType,
                    q.QuestionText,
                    q.QuestionOrder,
                    qo.OptionText AS Answer
                FROM 
                    SurveyResponses r
                LEFT JOIN
                    SurveyChannels sc ON r.ChannelId = sc.ChannelId
                INNER JOIN
                    ResponseAnswerOptions rao ON r.ResponseId = rao.ResponseId
                INNER JOIN
                    Questions q ON rao.QuestionId = q.QuestionId
                INNER JOIN
                    QuestionOptions qo ON rao.OptionId = qo.OptionId
                WHERE 
                    r.SurveyId = @SurveyId
                    AND r.Status = 'Submitted'
                    AND (@StartDate IS NULL OR r.SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR r.SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR r.ChannelId = @ChannelId)";

    var parameters = new
    {
        SurveyId = surveyId,
        StartDate = filters.StartDate,
        EndDate = filters.EndDate,
        ChannelId = filters.ChannelId
    };

    // Execute both queries
    var textAnswers = await connection.QueryAsync<dynamic>(textAnswersQuery, parameters);
    var optionAnswers = await connection.QueryAsync<dynamic>(optionAnswersQuery, parameters);

    // Merge results
    var allAnswers = textAnswers.Concat(optionAnswers)
        .OrderBy(x => (DateTime)x.SubmittedAt)
        .ThenBy(x => (Guid)x.ResponseId)
        .ThenBy(x => (int)x.QuestionOrder);

    // Group by ResponseId
    var responses = new List<RawResponseViewModel>();
    foreach (var group in allAnswers.GroupBy(x => (Guid)x.ResponseId))
    {
        var response = new RawResponseViewModel
        {
            ResponseId = group.Key,
            SubmittedAt = group.First().SubmittedAt,
            ChannelType = group.First().ChannelType,
            Answers = new Dictionary<string, string>()
        };

        foreach (var row in group.OrderBy(x => (int)x.QuestionOrder))
        {
            string questionText = row.QuestionText ?? "Unknown";
            string answer = row.Answer ?? "";
            
            if (!response.Answers.ContainsKey(questionText))
            {
                response.Answers[questionText] = answer;
            }
            else
            {
                // Multiple answers (checkboxes)
                response.Answers[questionText] += $", {answer}";
            }
        }

        responses.Add(response);
    }

    _logger.LogInformation("Retrieved {Count} raw responses with all answers", responses.Count);
    return responses;
        }

        public async Task<int> GetTotalResponsesAsync(Guid surveyId, FilterViewModel filters)
        {
            using var connection = new SqlConnection(_connectionString);
            
            var sql = @"
                SELECT COUNT(*)
                FROM SurveyResponses
                WHERE SurveyId = @SurveyId
                    AND (@StartDate IS NULL OR SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR ChannelId = @ChannelId)
                    AND (@Status IS NULL OR Status = @Status)";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                SurveyId = surveyId,
                StartDate = filters.StartDate,
                EndDate = filters.EndDate,
                ChannelId = filters.ChannelId,
                Status = filters.Status
            });
        }

        public async Task<int> GetCompletedResponsesAsync(Guid surveyId, FilterViewModel filters)
        {
            using var connection = new SqlConnection(_connectionString);
            
            var sql = @"
                SELECT COUNT(*)
                FROM SurveyResponses
                WHERE SurveyId = @SurveyId
                    AND Status = 'Submitted'
                    AND (@StartDate IS NULL OR SubmittedAtUtc >= @StartDate)
                    AND (@EndDate IS NULL OR SubmittedAtUtc <= @EndDate)
                    AND (@ChannelId IS NULL OR ChannelId = @ChannelId)";

            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                SurveyId = surveyId,
                StartDate = filters.StartDate,
                EndDate = filters.EndDate,
                ChannelId = filters.ChannelId
            });
        }
    }
}