using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class ActivityLog
{
    public long LogId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? SurveyId { get; set; }

    public Guid? ResponseId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? ActionDetail { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual SurveyResponse? Response { get; set; }

    public virtual Survey? Survey { get; set; }

    public virtual User? User { get; set; }
}
