using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class Survey
{
    public Guid SurveyId { get; set; }

    public Guid OwnerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? DefaultLanguage { get; set; }

    public bool IsAnonymous { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? OpenAtUtc { get; set; }

    public DateTime? CloseAtUtc { get; set; }

    public int? ResponseQuota { get; set; }

    public string? QuotaBehavior { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<BranchLogic> BranchLogics { get; set; } = new List<BranchLogic>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<SurveyChannel> SurveyChannels { get; set; } = new List<SurveyChannel>();

    public virtual ICollection<SurveyCollaborator> SurveyCollaborators { get; set; } = new List<SurveyCollaborator>();

    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
