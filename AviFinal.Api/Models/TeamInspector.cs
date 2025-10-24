using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class TeamInspector
{
    public int TeamInspectorId { get; set; }

    public int TeamId { get; set; }

    public string InspectorId { get; set; } = null!;

    public DateTime? AssignedDate { get; set; }

    public virtual LeaseCoUser Inspector { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
