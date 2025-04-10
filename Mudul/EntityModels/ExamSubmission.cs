using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class ExamSubmission
{
    public int SubmissionId { get; set; }

    public int ExamId { get; set; }

    public string StudentId { get; set; } = null!;

    public DateTime? SubmissionDate { get; set; }

    public string? ResponseData { get; set; }

    public string? GlobalResult { get; set; }

    public int? AttemptNumber { get; set; }

    public string Status { get; set; } = null!;

    public virtual Exam Exam { get; set; } = null!;

    public virtual AspNetUser Student { get; set; } = null!;

    public virtual ICollection<SubmissionDetail> SubmissionDetails { get; set; } = new List<SubmissionDetail>();
}
