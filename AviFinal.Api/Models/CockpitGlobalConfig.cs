using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class CockpitGlobalConfig
{
    public int Id { get; set; }

    public bool IsEnabled { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
