using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class LocoInput
{
    public int LocoNumber { get; set; }

    public string LocoType { get; set; } = null!;

    public string NetBookValue { get; set; } = null!;

    public string ScrapValue { get; set; } = null!;

    public string ScrappingCost { get; set; } = null!;

    public string? TotalCost { get; set; }

    public int LeaseTerm { get; set; }

    public string LeaseIncome { get; set; } = null!;

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
}
