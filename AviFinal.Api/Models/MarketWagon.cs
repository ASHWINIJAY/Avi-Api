using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class MarketWagon
{
    public int WagonNumber { get; set; }

    public string MarketValue { get; set; } = null!;
}
