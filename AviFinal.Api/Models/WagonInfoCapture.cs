using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class WagonInfoCapture
{
    public int Id { get; set; }

    public int WagonNumber { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string NetBookValue { get; set; } = null!;

    public string GpsLatitude { get; set; } = null!;

    public string GpsLongitude { get; set; } = null!;

    public string WagonPhoto { get; set; } = null!;

    public string BodyDamage { get; set; } = null!;

    public string? BodyPhoto1 { get; set; }

    public string? BodyPhoto2 { get; set; }

    public string? BodyPhoto3 { get; set; }

    public string WagonGroup { get; set; } = null!;

    public string BrakeType { get; set; } = null!;

    public string WagonType { get; set; } = null!;

    public string? LiftPhoto { get; set; }

    public string? LiftDate { get; set; }

    public string? LiftLapsed { get; set; }

    public string? BarrelPhoto { get; set; }

    public string? BarrelDate { get; set; }

    public string? BarrelLapsed { get; set; }

    public string? BrakePhoto { get; set; }

    public string? BrakeDate { get; set; }

    public string? BrakeLapsed { get; set; }
}
