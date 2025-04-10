using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class Enrollment
{
    public int EnrollmentId { get; set; }

    public string StudentId { get; set; } = null!;

    public int SubjectId { get; set; }

    public DateTime? EnrollmentDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual AspNetUser Student { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
