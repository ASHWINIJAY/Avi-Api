using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class LeaseCoUser
{
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string? Name { get; set; }

    public virtual ICollection<BatSwitchInspect> BatSwitchInspects { get; set; } = new List<BatSwitchInspect>();

    public virtual ICollection<BotLeftPanInspect> BotLeftPanInspects { get; set; } = new List<BotLeftPanInspect>();

    public virtual ICollection<CabLocoInspect> CabLocoInspects { get; set; } = new List<CabLocoInspect>();

    public virtual ICollection<CenAirInspect> CenAirInspects { get; set; } = new List<CenAirInspect>();

    public virtual ICollection<CirBreakPanInspect> CirBreakPanInspects { get; set; } = new List<CirBreakPanInspect>();

    public virtual ICollection<ComFanInspect> ComFanInspects { get; set; } = new List<ComFanInspect>();

    public virtual ICollection<CoupGearInspect> CoupGearInspects { get; set; } = new List<CoupGearInspect>();

    public virtual ICollection<E18inspect> E18inspects { get; set; } = new List<E18inspect>();

    public virtual ICollection<ElectCabInspect> ElectCabInspects { get; set; } = new List<ElectCabInspect>();

    public virtual ICollection<EndDeckInspect> EndDeckInspects { get; set; } = new List<EndDeckInspect>();

    public virtual ICollection<EngineDeckInspect> EngineDeckInspects { get; set; } = new List<EngineDeckInspect>();

    public virtual ICollection<FrontLocoInspect> FrontLocoInspects { get; set; } = new List<FrontLocoInspect>();

    public virtual ICollection<Ge34inspect> Ge34inspects { get; set; } = new List<Ge34inspect>();

    public virtual ICollection<Ge35inspect> Ge35inspects { get; set; } = new List<Ge35inspect>();

    public virtual ICollection<Ge36inspect> Ge36inspects { get; set; } = new List<Ge36inspect>();

    public virtual ICollection<LeftMidDoorInspect> LeftMidDoorInspects { get; set; } = new List<LeftMidDoorInspect>();

    public virtual ICollection<MidPanInspect> MidPanInspects { get; set; } = new List<MidPanInspect>();

    public virtual ICollection<RoofInspect> RoofInspects { get; set; } = new List<RoofInspect>();

    public virtual ICollection<ShortNoseInspect> ShortNoseInspects { get; set; } = new List<ShortNoseInspect>();

    public virtual ICollection<TeamInspector> TeamInspectors { get; set; } = new List<TeamInspector>();

    public virtual ICollection<TopRightPanInspect> TopRightPanInspects { get; set; } = new List<TopRightPanInspect>();

    public virtual ICollection<WalkAroundInspect> WalkAroundInspects { get; set; } = new List<WalkAroundInspect>();
}
