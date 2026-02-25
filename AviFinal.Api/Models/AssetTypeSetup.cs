using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

// ADJUSTED ↓
public partial class AssetTypeSetup
{
    public string AssetType { get; set; } = null!;

    public string LeaseIncome { get; set; } = null!;

    public string DateSaved { get; set; } = null!;

    public string SavedBy { get; set; } = null!;

    public int LeaseTerm { get; set; }

    public string EscalationRate { get; set; } = null!;

    public int UseAfterRefurbish { get; set; }

    public int WearTearPeriod { get; set; }

    public string OperatingCosts { get; set; } = null!;

    public string OperatingCostsEscalation { get; set; } = null!;

    public string CorporateTaxRate { get; set; } = null!;
}
