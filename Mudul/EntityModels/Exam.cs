using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class Exam
{
    public int ExamId { get; set; }

    public int SubjectId { get; set; }

    public string TeacherId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? PublishDate { get; set; }

    public string ExamType { get; set; } = null!;

    public int? TimeLimit { get; set; }

    public string? H5pcontentId { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public virtual ICollection<ExamSubmission> ExamSubmissions { get; set; } = new List<ExamSubmission>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual AspNetUser Teacher { get; set; } = null!;
}
