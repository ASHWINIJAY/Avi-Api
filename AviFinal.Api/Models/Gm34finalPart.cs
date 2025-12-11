using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class Gm34finalPart
{
    public int Id { get; set; }

    public string LocoModel { get; set; } = null!;

    public string FormId { get; set; } = null!;

    public string PartId { get; set; } = null!;

    public string PartDescr { get; set; } = null!;

    public string RefurbishValue { get; set; } = null!;

    public string MissingValue { get; set; } = null!;

    public string ReplaceValue { get; set; } = null!;

    public string? LabourValue { get; set; }
}
