using AviAppFinal.Server.Models;
using System;
using System.Collections.Generic;
using AviAppFinal.Server.Models;
namespace AviFinal.Api.Models;

public partial class WagonPartsInspect : IInspectWagonEntity
{
    public int Id { get; set; }

    public int WagonNumber { get; set; }

    public string WagonGroup { get; set; } = null!;

    public string WagonType { get; set; } = null!;

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

    public string? LaborValue { get; set; }

    public int Phase { get; set; }
}
