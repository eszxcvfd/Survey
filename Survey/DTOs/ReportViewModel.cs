namespace Survey.DTOs
{
    /// <summary>
    /// View model chứa toàn bộ dữ liệu báo cáo cho trang phân tích
    /// </summary>
    public class ReportViewModel
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public FilterViewModel Filters { get; set; } = new();
        public List<QuestionStatViewModel> QuestionStats { get; set; } = new();
        public int TotalResponses { get; set; }
        public int CompletedResponses { get; set; }
        public double AverageCompletionTime { get; set; }
        
        // ✅ MỚI: Thêm tỉ lệ trả lời câu hỏi
        public int TotalQuestions { get; set; }
        public int TotalAnsweredQuestions { get; set; }
        public double AnswerRate { get; set; } // % câu hỏi được trả lời
    }

    /// <summary>
    /// Bộ lọc cho báo cáo
    /// </summary>
    public class FilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? ChannelId { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Thống kê cho một câu hỏi
    /// </summary>
    public class QuestionStatViewModel
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public int TotalAnswers { get; set; }
        public List<ChartDataPoint> DataPoints { get; set; } = new();
    }

    /// <summary>
    /// Điểm dữ liệu cho biểu đồ
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả tổng hợp từ database
    /// </summary>
    public class AggregatedResultDto
    {
        public Guid QuestionId { get; set; }
        public Guid? OptionId { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    /// <summary>
    /// Dữ liệu thô cho việc xuất file
    /// </summary>
    public class RawResponseViewModel
    {
        public Guid ResponseId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? ChannelType { get; set; }
        public Dictionary<string, string> Answers { get; set; } = new();
    }
}