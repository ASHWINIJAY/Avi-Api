using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class AssetTypeSetup
{
    public string AssetType { get; set; } = null!;

    public string RefurbishmentCost { get; set; } = null!;

    public string LeaseIncome { get; set; } = null!;

    public string DateSaved { get; set; } = null!;

    public string SavedBy { get; set; } = null!;
}
