using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class NewLeaseIncome
{
    public string AssetType { get; set; } = null!;

    public string LeaseIncome { get; set; } = null!;
}
