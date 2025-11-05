using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class SurveyCollaborator
{
    public Guid SurveyId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime GrantedAtUtc { get; set; }

    public Guid? GrantedBy { get; set; }

    public virtual Survey Survey { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
