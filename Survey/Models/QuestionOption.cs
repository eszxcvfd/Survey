using System;
using System.Collections.Generic;

namespace Survey.Models;

public partial class QuestionOption
{
    public Guid OptionId { get; set; }

    public Guid QuestionId { get; set; }

    public int OptionOrder { get; set; }

    public string OptionText { get; set; } = null!;

    public string? OptionValue { get; set; }

    public bool IsActive { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<ResponseAnswerOption> ResponseAnswerOptions { get; set; } = new List<ResponseAnswerOption>();
}
