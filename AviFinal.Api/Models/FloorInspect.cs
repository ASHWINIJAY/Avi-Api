using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class FloorInspect
{
    public int Id { get; set; }

    public int WagonNumber { get; set; }

    public string WagonGroup { get; set; } = null!;

    public string WagonType { get; set; } = null!;

    public string FormId { get; set; } = null!;

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

    public int SectionQty { get; set; }
}
