using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class WaccSetup
{
    public int Id { get; set; }

    public string PostTax { get; set; } = null!;

    public string PreTax { get; set; } = null!;

    public string UpdateDate { get; set; } = null!;

    public string UpdateBy { get; set; } = null!;
}
