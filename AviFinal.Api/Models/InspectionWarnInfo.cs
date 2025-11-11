using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class InspectionWarnInfo
{
    public long Id { get; set; }

    public string? InspectionNumber { get; set; }

    public string? InspectionType { get; set; }

    public string? Username { get; set; }

    public string? Info { get; set; }

    public DateTime? CreatedTime { get; set; }

    public string? Lat { get; set; }

    public string? Long { get; set; }
}
