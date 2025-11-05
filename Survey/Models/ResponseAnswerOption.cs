using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class ResponseAnswerOption
{
    public Guid ResponseId { get; set; }

    public Guid QuestionId { get; set; }

    public Guid OptionId { get; set; }

    public int? RankOrder { get; set; }

    public string? AdditionalText { get; set; }

    public virtual QuestionOption Option { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual SurveyResponse Response { get; set; } = null!;
}
