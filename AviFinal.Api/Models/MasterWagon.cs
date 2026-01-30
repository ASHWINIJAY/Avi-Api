using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class MasterWagon
{
    public int WagonNumber { get; set; }

    public string InventoryNumber { get; set; } = null!;

    public string WagonType { get; set; } = null!;

    public string Mis { get; set; } = null!;

    public string? CommodityAssignedToTheWagons { get; set; }

    public string FinalDestDesc { get; set; } = null!;

    public string? ContentsCode { get; set; }

    public int WagonClass { get; set; }

    public string AssetVerificationLocation { get; set; } = null!;

    public string AssetVerificationStatus { get; set; } = null!;

    public string? ConditionRating { get; set; }

    public string Age { get; set; } = null!;

    public string LiftingCycle { get; set; } = null!;

    public string AssetRegister { get; set; } = null!;

    public string BrakeSystem { get; set; } = null!;

    public string Gauge { get; set; } = null!;

    public string WagonStatus { get; set; } = null!;

    public string LoadPlaceDesc { get; set; } = null!;

    public int? CurrentArea { get; set; }

    public string? CurrentLocationDesc { get; set; }

    public string Fleet { get; set; } = null!;

    public string AssignedTo { get; set; } = null!;

    public string? ReservedFor { get; set; }

    public string? CorridorName { get; set; }

    public int? HoursIdle { get; set; }

    public string? TrainNumber { get; set; }

    public string DepartureDate { get; set; } = null!;

    public DateOnly ArrivalDate { get; set; }

    public string TransitTime { get; set; } = null!;

    public int OverBorder { get; set; }

    public string? PrevContents { get; set; }

    public string ActiveStatus { get; set; } = null!;

    public string ArrivedOrEnroute { get; set; } = null!;

    public string LoadedOrEmptyOrScrap { get; set; } = null!;

    public string? ReaderNumber { get; set; }

    public string? ReaderPlaceCode { get; set; }

    public DateTime? ReaderLastReport { get; set; }

    public string? ReaderName { get; set; }

    public string? ReaderReportStatus { get; set; }

    public string? AcqusitionValue { get; set; }

    public string? NetBookValue { get; set; }

    public string? ScrapValue { get; set; }

    public string HsbogiesFitted { get; set; } = null!;

    public string NextLifting { get; set; } = null!;

    public int NextLifting2 { get; set; }

    public int IsApressureVessel { get; set; }

    public string NextBarrelTest { get; set; } = null!;

    public string UpdatedStatus2 { get; set; } = null!;

    public DateTime NotificationCreatedOn { get; set; }

    public bool WorkshopBasedOnNotification { get; set; }

    public string UpdatedStatus3 { get; set; } = null!;

    public bool DeemedActive { get; set; }

    public string ReturnToServiceInterventionCategory { get; set; } = null!;

    public string NotificationDescription { get; set; } = null!;

    public bool DueBasedOnNotification { get; set; }

    public bool DepotUnscheduled { get; set; }

    public int BarrelTestDue { get; set; }

    public bool Workshop { get; set; }

    public int Wreckage { get; set; }

    public int Scrap { get; set; }

    public bool Available { get; set; }

    public string DepotUnschedCost { get; set; } = null!;

    public string DepotSchedCost { get; set; } = null!;

    public int BarrelTestCosts { get; set; }

    public string WorkshopCost { get; set; } = null!;

    public string YearsStanding { get; set; } = null!;

    public bool MonthsStanding { get; set; }

    public string DeemedActiveStandingTimePenaltyCost { get; set; } = null!;

    public string TotalReturnToServiceEstimatedCost { get; set; } = null!;

    public int LiftingCycle2 { get; set; }

    public bool RequiresOnlyDepotLevelUnscheduledIntervention { get; set; }

    public string NextLiftingAfterReturningToService { get; set; } = null!;

    public int NextLiftingDueInYears { get; set; }

    public string _1 { get; set; } = null!;

    public string _2 { get; set; } = null!;

    public string _3 { get; set; } = null!;

    public string _4 { get; set; } = null!;

    public string _5 { get; set; } = null!;

    public string _6 { get; set; } = null!;

    public string _7 { get; set; } = null!;

    public string _8 { get; set; } = null!;

    public string _9 { get; set; } = null!;

    public string _10 { get; set; } = null!;

    public bool KnownUnscheduled { get; set; }

    public bool DeemedUnscheduled { get; set; }

    public bool Scheduled { get; set; }

    public int BarrelTest { get; set; }

    public bool Workshop2 { get; set; }

    public int Wreck { get; set; }

    public int Scrap2 { get; set; }

    public int TotalReturnToServiceInterventions { get; set; }

    public string RevisedBrakeSystem { get; set; } = null!;

    public int VacuumBrakeConversion { get; set; }

    public string TotalRts { get; set; } = null!;

    public string RevisedCommodity { get; set; } = null!;

    public int CommodityScore { get; set; }

    public int ConditionScore { get; set; }

    public int BrakingScore { get; set; }

    public int CombinedScores { get; set; }

    public int YearOfRts { get; set; }

    public string RevisedRtscost { get; set; } = null!;

    public int RevisedYearOfRts { get; set; }

    public string Decision { get; set; } = null!;

    public string? MarketValue { get; set; }

    public int? TareWeight { get; set; }
}
