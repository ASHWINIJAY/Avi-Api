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

    public virtual ICollection<BatSwitchInspect> BatSwitchInspects { get; set; } = new List<BatSwitchInspect>();

    public virtual ICollection<BotLeftPanInspect> BotLeftPanInspects { get; set; } = new List<BotLeftPanInspect>();

    public virtual ICollection<CabLocoInspect> CabLocoInspects { get; set; } = new List<CabLocoInspect>();

    public virtual ICollection<CenAirInspect> CenAirInspects { get; set; } = new List<CenAirInspect>();

    public virtual ICollection<CirBreakPanInspect> CirBreakPanInspects { get; set; } = new List<CirBreakPanInspect>();

    public virtual ICollection<ComFanInspect> ComFanInspects { get; set; } = new List<ComFanInspect>();

    public virtual ICollection<CoupGearInspect> CoupGearInspects { get; set; } = new List<CoupGearInspect>();

    public virtual ICollection<ElectCabInspect> ElectCabInspects { get; set; } = new List<ElectCabInspect>();

    public virtual ICollection<EndDeckInspect> EndDeckInspects { get; set; } = new List<EndDeckInspect>();

    public virtual ICollection<EngineDeckInspect> EngineDeckInspects { get; set; } = new List<EngineDeckInspect>();

    public virtual ICollection<FrontLocoInspect> FrontLocoInspects { get; set; } = new List<FrontLocoInspect>();

    public virtual ICollection<LeftMidDoorInspect> LeftMidDoorInspects { get; set; } = new List<LeftMidDoorInspect>();

    public virtual ICollection<MidPanInspect> MidPanInspects { get; set; } = new List<MidPanInspect>();

    public virtual ICollection<RoofInspect> RoofInspects { get; set; } = new List<RoofInspect>();

    public virtual ICollection<ShortNoseInspect> ShortNoseInspects { get; set; } = new List<ShortNoseInspect>();

    public virtual ICollection<TopRightPanInspect> TopRightPanInspects { get; set; } = new List<TopRightPanInspect>();

    public virtual ICollection<WalkAroundInspect> WalkAroundInspects { get; set; } = new List<WalkAroundInspect>();
}
