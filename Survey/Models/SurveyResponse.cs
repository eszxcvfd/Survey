using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class SurveyResponse
{
    public Guid ResponseId { get; set; }

    public Guid SurveyId { get; set; }

    public Guid? ChannelId { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime LastUpdatedAtUtc { get; set; }

    public string Status { get; set; } = null!;

    public string? AnonToken { get; set; }

    public string? RespondentEmail { get; set; }

    public string? RespondentIP { get; set; }

    public string? AntiSpamToken { get; set; }

    public bool IsLocked { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual SurveyChannel? Channel { get; set; }

    public virtual ICollection<ResponseAnswerOption> ResponseAnswerOptions { get; set; } = new List<ResponseAnswerOption>();

    public virtual ICollection<ResponseAnswer> ResponseAnswers { get; set; } = new List<ResponseAnswer>();

    public virtual Survey Survey { get; set; } = null!;
}
