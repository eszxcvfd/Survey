using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class BranchLogic
{
    public Guid LogicId { get; set; }

    public Guid SurveyId { get; set; }

    public Guid SourceQuestionId { get; set; }

    public string ConditionExpr { get; set; } = null!;

    public string TargetAction { get; set; } = null!;

    public Guid? TargetQuestionId { get; set; }

    public int PriorityOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Question SourceQuestion { get; set; } = null!;

    public virtual Survey Survey { get; set; } = null!;

    public virtual Question? TargetQuestion { get; set; }
}
