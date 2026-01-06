using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class CockpitAllocation
{
    public int AllocationId { get; set; }

    public string AssetType { get; set; } = null!;

    public int TeamId { get; set; }

    public int AssetId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string RefNo { get; set; } = null!;
}
