using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

// ADJUSTED ↓
public partial class WagonInput
{
    public int WagonNumber { get; set; }

    public string WagonType { get; set; } = null!;

    public string NetBookValue { get; set; } = null!;

    public string ScrapValue { get; set; } = null!;

    public string ScrappingCost { get; set; } = null!;

    public string? TotalCost { get; set; }

    public int LeaseTerm { get; set; }

    public string? LeaseIncome { get; set; }

    public string EscalationRate { get; set; } = null!;

    public int UseAfterRefurbish { get; set; }

    public string ResidualValue { get; set; } = null!;

    public string PostTax { get; set; } = null!;

    public int WearTearPeriod { get; set; }

    public string OperatingCosts { get; set; } = null!;

    public string OperatingCostsEscalation { get; set; } = null!;

    public string CorporateTaxRate { get; set; } = null!;

    public string PreTax { get; set; } = null!;

    public string DateSaved { get; set; } = null!;

    public string SavedBy { get; set; } = null!;

    public string NewScrapValue { get; set; } = null!;

    public string? InspectStatus { get; set; }

    public string? MarketValue { get; set; } = null!;

    public string? BenchmarkValue { get; set; } = null!;
}
