using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class D35locos
{
    public int Num { get; set; }

    public int AssetCode { get; set; }

    public string LocoClass { get; set; } = null!;

    public string? LocoType { get; set; }

    public string? InventoryNumber { get; set; }

    public string? NetBookValue { get; set; }

    public string? LocoModel { get; set; }

    public virtual MasterLocos AssetCodeNavigation { get; set; } = null!;
}
