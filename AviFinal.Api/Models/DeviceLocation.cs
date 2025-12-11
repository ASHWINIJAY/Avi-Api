using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class DeviceLocation
{
    public int Id { get; set; }

    public string? DeviceId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? Accuracy { get; set; }

    public DateTime? DeviceTimestamp { get; set; }

    public DateTime? ServerTimestamp { get; set; }

    public string? UserName { get; set; }
}
