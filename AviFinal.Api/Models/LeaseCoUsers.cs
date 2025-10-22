using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class LeaseCoUsers
{
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string? Name { get; set; }

    public virtual ICollection<BatSwitchInspects> BatSwitchInspects { get; set; } = new List<BatSwitchInspects>();

    public virtual ICollection<BotLeftPanInspects> BotLeftPanInspects { get; set; } = new List<BotLeftPanInspects>();

    public virtual ICollection<CabLocoInspects> CabLocoInspects { get; set; } = new List<CabLocoInspects>();

    public virtual ICollection<CenAirInspects> CenAirInspects { get; set; } = new List<CenAirInspects>();

    public virtual ICollection<CirBreakPanInspects> CirBreakPanInspects { get; set; } = new List<CirBreakPanInspects>();

    public virtual ICollection<ComFanInspects> ComFanInspects { get; set; } = new List<ComFanInspects>();

    public virtual ICollection<CoupGearInspects> CoupGearInspects { get; set; } = new List<CoupGearInspects>();

    public virtual ICollection<E18inspects> E18inspects { get; set; } = new List<E18inspects>();

    public virtual ICollection<ElectCabInspects> ElectCabInspects { get; set; } = new List<ElectCabInspects>();

    public virtual ICollection<EndDeckInspects> EndDeckInspects { get; set; } = new List<EndDeckInspects>();

    public virtual ICollection<EngineDeckInspects> EngineDeckInspects { get; set; } = new List<EngineDeckInspects>();

    public virtual ICollection<FrontLocoInspects> FrontLocoInspects { get; set; } = new List<FrontLocoInspects>();

    public virtual ICollection<Ge34inspects> Ge34inspects { get; set; } = new List<Ge34inspects>();

    public virtual ICollection<Ge35inspects> Ge35inspects { get; set; } = new List<Ge35inspects>();

    public virtual ICollection<Ge36inspects> Ge36inspects { get; set; } = new List<Ge36inspects>();

    public virtual ICollection<LeftMidDoorInspects> LeftMidDoorInspects { get; set; } = new List<LeftMidDoorInspects>();

    public virtual ICollection<MidPanInspects> MidPanInspects { get; set; } = new List<MidPanInspects>();

    public virtual ICollection<RoofInspects> RoofInspects { get; set; } = new List<RoofInspects>();

    public virtual ICollection<ShortNoseInspects> ShortNoseInspects { get; set; } = new List<ShortNoseInspects>();

    public virtual ICollection<TopRightPanInspects> TopRightPanInspects { get; set; } = new List<TopRightPanInspects>();

    public virtual ICollection<WalkAroundInspects> WalkAroundInspects { get; set; } = new List<WalkAroundInspects>();
}
