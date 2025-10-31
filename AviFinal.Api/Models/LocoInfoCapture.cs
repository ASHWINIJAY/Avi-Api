using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class LocoInfoCapture
{
    public int Id { get; set; }

    public int LocoNumber { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string NetBookValue { get; set; } = null!;

    public string GpsLatitude { get; set; } = null!;

    public string GpsLongitude { get; set; } = null!;

    public string LocoPhoto { get; set; } = null!;

    public string BodyDamage { get; set; } = null!;

    public string? BodyPhoto1 { get; set; }

    public string? BodyPhoto2 { get; set; }

    public string? BodyPhoto3 { get; set; }

    public string LocoClass { get; set; } = null!;

    public string LocoModel { get; set; } = null!;

    public string LiftPhoto { get; set; } = null!;

    public string LiftDate { get; set; } = null!;
}
