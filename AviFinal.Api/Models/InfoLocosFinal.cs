using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AviFinal.Api.Models;

[Table("InfoLocosFinal")]
public partial class InfoLocosFinal
{
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string InfoId { get; set; } = null!;

    public int LocoNumber { get; set; }

    public string BodyDamage { get; set; } = null!;

    public string? BodyRepairValue { get; set; }

    public string LiftingRequired { get; set; } = null!;

    public DateOnly? LiftDate { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string LocoType { get; set; } = null!;

    public string? NetBookValue { get; set; }

    public string ProMain { get; set; } = null!;

    public string GpsLatitude { get; set; } = null!;

    public string GpsLongitude { get; set; } = null!;

    public string FleetRenewPro { get; set; } = null!;

    public string PhotoPath { get; set; } = null!;

    public string? BodyPhotoPaths { get; set; }

    public string? LiftingPhotoPaths { get; set; }

    public virtual MasterLoco LocoNumberNavigation { get; set; } = null!;
}
