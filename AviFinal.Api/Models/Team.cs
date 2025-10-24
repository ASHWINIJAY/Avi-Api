using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class Team
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<TeamInspector> TeamInspectors { get; set; } = new List<TeamInspector>();
}
