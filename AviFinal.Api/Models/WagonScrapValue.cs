using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class WagonScrapValue
{
    public int WagonNumber { get; set; }

    public int TareWeight { get; set; }

    public string ScrapValue { get; set; } = null!;
}
