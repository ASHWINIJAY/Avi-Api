using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class ElectCabInspects
{
    public string ItemId { get; set; } = null!;

    public string InspectFormId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string PartDescr { get; set; } = null!;

    public string GoodCheck { get; set; } = null!;

    public string RefurbishCheck { get; set; } = null!;

    public string MissingCheck { get; set; } = null!;

    public string DamageCheck { get; set; } = null!;

    public string? ReplaceCost { get; set; }

    public string? RefurbishCost { get; set; }

    public int LocoNumber { get; set; }

    public virtual MasterLocos LocoNumberNavigation { get; set; } = null!;

    public virtual LeaseCoUsers User { get; set; } = null!;
}
