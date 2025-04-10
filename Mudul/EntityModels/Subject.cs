using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class Subject
{
    public int SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? Year { get; set; }

    public int AreaId { get; set; }

    public string TeacherId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual Area Area { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual AspNetUser Teacher { get; set; } = null!;
}
