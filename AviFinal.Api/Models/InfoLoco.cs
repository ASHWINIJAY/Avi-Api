using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class InfoLoco
{
    public string InfoId { get; set; } = null!;

    public int LocoNumber { get; set; }

    public string BodyDamage { get; set; } = null!;

    public string? BodyRepairValue { get; set; }

    public string LiftingRequired { get; set; } = null!;

    public DateOnly? LiftDate { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string LocoType { get; set; } = null!;

    public string? NetBookValue { get; set; }

    public string ProMain { get; set; } = null!;

    public string GpsLatitude { get; set; } = null!;

    public string GpsLongitude { get; set; } = null!;

    public string FleetRenewPro { get; set; } = null!;

    public byte[] PhotoPath { get; set; } = null!;

    public byte[]? BodyPhotoPaths { get; set; }

    public byte[]? LiftingPhotoPaths { get; set; }

    public virtual MasterLoco LocoNumberNavigation { get; set; } = null!;
}
