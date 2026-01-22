using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class ManualDcfinput
{
    public long Id { get; set; }

    public decimal? AssetNumber { get; set; }

    public string? AssetType { get; set; }

    public string? ScrapValue { get; set; }

    public string? RefurbishValue { get; set; }

    public string? TransferValue { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }
}
