using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class SurveyChannel
{
    public Guid ChannelId { get; set; }

    public Guid SurveyId { get; set; }

    public string ChannelType { get; set; } = null!;

    public string? PublicUrlSlug { get; set; }

    public string? FullUrl { get; set; }

    public string? QrImagePath { get; set; }

    public string? EmailSubject { get; set; }

    public string? EmailBody { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Survey Survey { get; set; } = null!;

    public virtual ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
}
