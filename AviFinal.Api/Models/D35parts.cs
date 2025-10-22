using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class D35parts
{
    public int Id { get; set; }

    public string FormId { get; set; } = null!;

    public string PartDescr { get; set; } = null!;

    public string RefurbishValue { get; set; } = null!;

    public string MissingValue { get; set; } = null!;

    public string ReplaceValue { get; set; } = null!;
}
