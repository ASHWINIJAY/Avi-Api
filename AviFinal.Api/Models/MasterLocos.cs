using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class MasterLocos
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

    public virtual ICollection<BatSwitchInspects> BatSwitchInspects { get; set; } = new List<BatSwitchInspects>();

    public virtual ICollection<BotLeftPanInspects> BotLeftPanInspects { get; set; } = new List<BotLeftPanInspects>();

    public virtual ICollection<CabLocoInspects> CabLocoInspects { get; set; } = new List<CabLocoInspects>();

    public virtual ICollection<CenAirInspects> CenAirInspects { get; set; } = new List<CenAirInspects>();

    public virtual ICollection<CirBreakPanInspects> CirBreakPanInspects { get; set; } = new List<CirBreakPanInspects>();

    public virtual ICollection<ComFanInspects> ComFanInspects { get; set; } = new List<ComFanInspects>();

    public virtual ICollection<CoupGearInspects> CoupGearInspects { get; set; } = new List<CoupGearInspects>();

    public virtual ICollection<D34locos> D34locos { get; set; } = new List<D34locos>();

    public virtual ICollection<D35locos> D35locos { get; set; } = new List<D35locos>();

    public virtual ICollection<D36locos> D36locos { get; set; } = new List<D36locos>();

    public virtual ICollection<DashBoardItems> DashBoardItems { get; set; } = new List<DashBoardItems>();

    public virtual ICollection<E18inspects> E18inspects { get; set; } = new List<E18inspects>();

    public virtual ICollection<ElectCabInspects> ElectCabInspects { get; set; } = new List<ElectCabInspects>();

    public virtual ICollection<EndDeckInspects> EndDeckInspects { get; set; } = new List<EndDeckInspects>();

    public virtual ICollection<EngineDeckInspects> EngineDeckInspects { get; set; } = new List<EngineDeckInspects>();

    public virtual ICollection<FrontLocoInspects> FrontLocoInspects { get; set; } = new List<FrontLocoInspects>();

    public virtual ICollection<Ge34inspects> Ge34inspects { get; set; } = new List<Ge34inspects>();

    public virtual ICollection<Ge35inspects> Ge35inspects { get; set; } = new List<Ge35inspects>();

    public virtual ICollection<Ge36inspects> Ge36inspects { get; set; } = new List<Ge36inspects>();

    public virtual ICollection<InfoLocos> InfoLocos { get; set; } = new List<InfoLocos>();

    public virtual ICollection<InfoLocosFinal> InfoLocosFinal { get; set; } = new List<InfoLocosFinal>();

    public virtual ICollection<LeftMidDoorInspects> LeftMidDoorInspects { get; set; } = new List<LeftMidDoorInspects>();

    public virtual ICollection<MidPanInspects> MidPanInspects { get; set; } = new List<MidPanInspects>();

    public virtual ICollection<RoofInspects> RoofInspects { get; set; } = new List<RoofInspects>();

    public virtual ICollection<ShortNoseInspects> ShortNoseInspects { get; set; } = new List<ShortNoseInspects>();

    public virtual ICollection<TopRightPanInspects> TopRightPanInspects { get; set; } = new List<TopRightPanInspects>();

    public virtual ICollection<WalkAroundInspects> WalkAroundInspects { get; set; } = new List<WalkAroundInspects>();
}
