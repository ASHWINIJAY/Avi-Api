using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class MasterLoco
{
    public int LocoNumber { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string LocoType { get; set; } = null!;

    public string LocoClass { get; set; } = null!;

    public string AvailNov24 { get; set; } = null!;

    public string ConditionNov24 { get; set; } = null!;

    public string ConditionsSummary { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string WorkScopeToRtsimmediately { get; set; } = null!;

    public decimal ReturnToServiceCostAsIs { get; set; }

    public int TimeToRtsmonths { get; set; }

    public string Corridors { get; set; } = null!;

    public string Home { get; set; } = null!;

    public string? Sdep { get; set; }

    public DateOnly? Sdate { get; set; }

    public string? RepairC { get; set; }

    public string? Stype { get; set; }

    public string? Lstat { get; set; }

    public DateOnly? Rdate { get; set; }

    public string? Remark { get; set; }

    public string? Descrip { get; set; }

    public string? HoursS { get; set; }

    public string? HoursT { get; set; }

    public int? Train { get; set; }

    public string? Dest { get; set; }

    public DateTime? Depart { get; set; }

    public string? Move { get; set; }

    public DateTime? Arrive { get; set; }

    public string? Org { get; set; }

    public decimal? AcqusitionValue { get; set; }

    public string? NetBookValue { get; set; }

    public decimal? ScrapValue { get; set; }

    public string _619 { get; set; } = null!;

    public string? CompareLocosAgainst695List { get; set; }

    public virtual ICollection<BatSwitchInspect> BatSwitchInspects { get; set; } = new List<BatSwitchInspect>();

    public virtual ICollection<BotLeftPanInspect> BotLeftPanInspects { get; set; } = new List<BotLeftPanInspect>();

    public virtual ICollection<CabLocoInspect> CabLocoInspects { get; set; } = new List<CabLocoInspect>();

    public virtual ICollection<CenAirInspect> CenAirInspects { get; set; } = new List<CenAirInspect>();

    public virtual ICollection<CirBreakPanInspect> CirBreakPanInspects { get; set; } = new List<CirBreakPanInspect>();

    public virtual ICollection<ComFanInspect> ComFanInspects { get; set; } = new List<ComFanInspect>();

    public virtual ICollection<CoupGearInspect> CoupGearInspects { get; set; } = new List<CoupGearInspect>();

    public virtual ICollection<DashBoardItem> DashBoardItems { get; set; } = new List<DashBoardItem>();

    public virtual ICollection<ElectCabInspect> ElectCabInspects { get; set; } = new List<ElectCabInspect>();

    public virtual ICollection<EndDeckInspect> EndDeckInspects { get; set; } = new List<EndDeckInspect>();

    public virtual ICollection<EngineDeckInspect> EngineDeckInspects { get; set; } = new List<EngineDeckInspect>();

    public virtual ICollection<FrontLocoInspect> FrontLocoInspects { get; set; } = new List<FrontLocoInspect>();

    public virtual ICollection<InfoLoco> InfoLocos { get; set; } = new List<InfoLoco>();

    public virtual ICollection<InfoLocosFinal> InfoLocosFinals { get; set; } = new List<InfoLocosFinal>();

    public virtual ICollection<LeftMidDoorInspect> LeftMidDoorInspects { get; set; } = new List<LeftMidDoorInspect>();

    public virtual ICollection<MidPanInspect> MidPanInspects { get; set; } = new List<MidPanInspect>();

    public virtual ICollection<RoofInspect> RoofInspects { get; set; } = new List<RoofInspect>();

    public virtual ICollection<ShortNoseInspect> ShortNoseInspects { get; set; } = new List<ShortNoseInspect>();

    public virtual ICollection<TopRightPanInspect> TopRightPanInspects { get; set; } = new List<TopRightPanInspect>();

    public virtual ICollection<WalkAroundInspect> WalkAroundInspects { get; set; } = new List<WalkAroundInspect>();
}
