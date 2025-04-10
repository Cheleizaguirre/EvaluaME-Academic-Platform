using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class ExamQuestion
{
    public int QuestionId { get; set; }

    public int ExamId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public string? JobeConfiguration { get; set; }

    public decimal? Weight { get; set; }

    public string Status { get; set; } = null!;

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();
}
