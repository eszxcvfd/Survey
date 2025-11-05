using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public string? ResetToken { get; set; }

    public DateTime? ResetTokenExpiry { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<SurveyCollaborator> SurveyCollaborators { get; set; } = new List<SurveyCollaborator>();

    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
