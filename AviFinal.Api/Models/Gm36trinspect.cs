using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class Gm36trinspect
{
    public int Id { get; set; }

    public int LocoNumber { get; set; }

    public string LocoClass { get; set; } = null!;

    public string LocoModel { get; set; } = null!;

    public string FormId { get; set; } = null!;

    public string PartId { get; set; } = null!;

    public string PartDescr { get; set; } = null!;

    public string GoodCheck { get; set; } = null!;

    public string RefurbishCheck { get; set; } = null!;

    public string MissingCheck { get; set; } = null!;

    public string ReplaceCheck { get; set; } = null!;

    public string? RefurbishValue { get; set; }

    public string? MissingValue { get; set; }

    public string? MissingPhoto { get; set; }

    public string? ReplaceValue { get; set; }

    public string? ReplacePhoto { get; set; }
}
