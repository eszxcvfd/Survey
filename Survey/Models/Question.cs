using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class Question
{
    public Guid QuestionId { get; set; }

    public Guid SurveyId { get; set; }

    public int QuestionOrder { get; set; }

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public bool IsRequired { get; set; }

    public string? ValidationRule { get; set; }

    public string? HelpText { get; set; }

    public string? DefaultValue { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public virtual ICollection<BranchLogic> BranchLogicSourceQuestions { get; set; } = new List<BranchLogic>();

    public virtual ICollection<BranchLogic> BranchLogicTargetQuestions { get; set; } = new List<BranchLogic>();

    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();

    public virtual ICollection<ResponseAnswerOption> ResponseAnswerOptions { get; set; } = new List<ResponseAnswerOption>();

    public virtual ICollection<ResponseAnswer> ResponseAnswers { get; set; } = new List<ResponseAnswer>();

    public virtual Survey Survey { get; set; } = null!;
}
