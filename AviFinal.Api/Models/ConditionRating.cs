using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class ConditionRating
{
    public int Score { get; set; }

    public string Condition { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string OperationalStatus { get; set; } = null!;
}
