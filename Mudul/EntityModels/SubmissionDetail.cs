using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class SubmissionDetail
{
    public int DetailId { get; set; }

    public int SubmissionId { get; set; }

    public int QuestionId { get; set; }

    public string? StudentAnswer { get; set; }

    public string? ValidationData { get; set; }

    public decimal? QuestionScore { get; set; }

    public string Status { get; set; } = null!;

    public virtual ExamQuestion Question { get; set; } = null!;

    public virtual ExamSubmission Submission { get; set; } = null!;
}
