using System;
using System.Collections.Generic;

namespace Mudul.EntityModels;

public partial class Area
{
    public int AreaId { get; set; }

    public string Name { get; set; } = null!;

    public string CoordinatorId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual AspNetUser Coordinator { get; set; } = null!;

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
