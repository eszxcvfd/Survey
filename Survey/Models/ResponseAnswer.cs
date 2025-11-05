using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class ResponseAnswer
{
    public Guid ResponseId { get; set; }

    public Guid QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public decimal? NumericValue { get; set; }

    public DateTime? DateValue { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual SurveyResponse Response { get; set; } = null!;
}
