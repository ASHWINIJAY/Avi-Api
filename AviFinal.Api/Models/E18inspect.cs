using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class E18inspect
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int LocoNumber { get; set; }

    public string LocoClass { get; set; } = null!;

    public string LocoModel { get; set; } = null!;

    public string FormId { get; set; } = null!;

    public string PartId { get; set; } = null!;

    public string PartDescr { get; set; } = null!;

    public string? GoodCheck { get; set; }

    public string? RefurbishCheck { get; set; }

    public string? MissingCheck { get; set; }

    public string? ReplaceCheck { get; set; }

    public string? RefurbishValue { get; set; }

    public string? MissingValue { get; set; }

    public string? ReplaceValue { get; set; }

    public string? DamagePhoto { get; set; }

    public string? MissingPhoto { get; set; }

    public virtual MasterLoco LocoNumberNavigation { get; set; } = null!;

    public virtual LeaseCoUser User { get; set; } = null!;
}
