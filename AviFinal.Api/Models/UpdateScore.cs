using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class UpdateScore
{
    public int AssetNumber { get; set; }

    public int ConditionScore { get; set; }

    public string OperationalStatus { get; set; } = null!;
}
