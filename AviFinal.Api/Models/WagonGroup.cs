using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class WagonGroup
{
    public int Id { get; set; }

    public string Group { get; set; } = null!;

    public int Qty { get; set; }

    public string? Type { get; set; }

    public string? AirBrake { get; set; }

    public string? VacuumBrake { get; set; }

    public string? DualBrake { get; set; }

    public string? Doors { get; set; }

    public string? Stanchions { get; set; }

    public string? Twistlocks { get; set; }
}
