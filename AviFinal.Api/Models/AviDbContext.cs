using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Models;

public partial class AviDbContext : DbContext
{
    public AviDbContext()
    {
    }

    public AviDbContext(DbContextOptions<AviDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AirBrakeFinalPart> AirBrakeFinalParts { get; set; }

    public virtual DbSet<AirBrakePartsInspect> AirBrakePartsInspects { get; set; }

    public virtual DbSet<AssetTypeSetup> AssetTypeSetups { get; set; }

    public virtual DbSet<BatSwitchInspect> BatSwitchInspects { get; set; }

    public virtual DbSet<BotLeftPanInspect> BotLeftPanInspects { get; set; }

    public virtual DbSet<BottomDischargeInspect> BottomDischargeInspects { get; set; }

    public virtual DbSet<CabLocoInspect> CabLocoInspects { get; set; }

    public virtual DbSet<CenAirInspect> CenAirInspects { get; set; }

    public virtual DbSet<CirBreakPanInspect> CirBreakPanInspects { get; set; }

    public virtual DbSet<CockpitAllocation> CockpitAllocations { get; set; }

    public virtual DbSet<CockpitGlobalConfig> CockpitGlobalConfigs { get; set; }

    public virtual DbSet<ComFanInspect> ComFanInspects { get; set; }

    public virtual DbSet<ConditionRating> ConditionRatings { get; set; }

    public virtual DbSet<CoupGearInspect> CoupGearInspects { get; set; }

    public virtual DbSet<D34loco> D34locos { get; set; }

    public virtual DbSet<D34part> D34parts { get; set; }

    public virtual DbSet<D35loco> D35locos { get; set; }

    public virtual DbSet<D35part> D35parts { get; set; }

    public virtual DbSet<D36loco> D36locos { get; set; }

    public virtual DbSet<DashBoardItem> DashBoardItems { get; set; }

    public virtual DbSet<DeviceLocation> DeviceLocations { get; set; }

    public virtual DbSet<DoorsInspect> DoorsInspects { get; set; }

    public virtual DbSet<E18bdinspect> E18bdinspects { get; set; }

    public virtual DbSet<E18beinspect> E18beinspects { get; set; }

    public virtual DbSet<E18ccinspect> E18ccinspects { get; set; }

    public virtual DbSet<E18crinspect> E18crinspects { get; set; }

    public virtual DbSet<E18ctinspect> E18ctinspects { get; set; }

    public virtual DbSet<E18eeinspect> E18eeinspects { get; set; }

    public virtual DbSet<E18ehinspect> E18ehinspects { get; set; }

    public virtual DbSet<E18esinspect> E18esinspects { get; set; }

    public virtual DbSet<E18finalPart> E18finalParts { get; set; }

    public virtual DbSet<E18flinspect> E18flinspects { get; set; }

    public virtual DbSet<E18hcinspect> E18hcinspects { get; set; }

    public virtual DbSet<E18hsinspect> E18hsinspects { get; set; }

    public virtual DbSet<E18hvinspect> E18hvinspects { get; set; }

    public virtual DbSet<E18inspect> E18inspects { get; set; }

    public virtual DbSet<E18loco> E18locos { get; set; }

    public virtual DbSet<E18lvinspect> E18lvinspects { get; set; }

    public virtual DbSet<E18mainspect> E18mainspects { get; set; }

    public virtual DbSet<E18mbinspect> E18mbinspects { get; set; }

    public virtual DbSet<E18rfinspect> E18rfinspects { get; set; }

    public virtual DbSet<ElectCabInspect> ElectCabInspects { get; set; }

    public virtual DbSet<EndDeckInspect> EndDeckInspects { get; set; }

    public virtual DbSet<EngineDeckInspect> EngineDeckInspects { get; set; }

    public virtual DbSet<FloorInspect> FloorInspects { get; set; }

    public virtual DbSet<FrontLocoInspect> FrontLocoInspects { get; set; }

    public virtual DbSet<Ge34acinspect> Ge34acinspects { get; set; }

    public virtual DbSet<Ge34bcinspect> Ge34bcinspects { get; set; }

    public virtual DbSet<Ge34bdinspect> Ge34bdinspects { get; set; }

    public virtual DbSet<Ge34bsinspect> Ge34bsinspects { get; set; }

    public virtual DbSet<Ge34cfinspect> Ge34cfinspects { get; set; }

    public virtual DbSet<Ge34clinspect> Ge34clinspects { get; set; }

    public virtual DbSet<Ge34deinspect> Ge34deinspects { get; set; }

    public virtual DbSet<Ge34ecinspect> Ge34ecinspects { get; set; }

    public virtual DbSet<Ge34edinspect> Ge34edinspects { get; set; }

    public virtual DbSet<Ge34finalPart> Ge34finalParts { get; set; }

    public virtual DbSet<Ge34flinspect> Ge34flinspects { get; set; }

    public virtual DbSet<Ge34inspect> Ge34inspects { get; set; }

    public virtual DbSet<Ge34odinspect> Ge34odinspects { get; set; }

    public virtual DbSet<Ge34rfinspect> Ge34rfinspects { get; set; }

    public virtual DbSet<Ge34sninspect> Ge34sninspects { get; set; }

    public virtual DbSet<Ge35bcinspect> Ge35bcinspects { get; set; }

    public virtual DbSet<Ge35bdinspect> Ge35bdinspects { get; set; }

    public virtual DbSet<Ge35bsinspect> Ge35bsinspects { get; set; }

    public virtual DbSet<Ge35cfinspect> Ge35cfinspects { get; set; }

    public virtual DbSet<Ge35clinspect> Ge35clinspects { get; set; }

    public virtual DbSet<Ge35deinspect> Ge35deinspects { get; set; }

    public virtual DbSet<Ge35ecinspect> Ge35ecinspects { get; set; }

    public virtual DbSet<Ge35edinspect> Ge35edinspects { get; set; }

    public virtual DbSet<Ge35finalPart> Ge35finalParts { get; set; }

    public virtual DbSet<Ge35flinspect> Ge35flinspects { get; set; }

    public virtual DbSet<Ge35inspect> Ge35inspects { get; set; }

    public virtual DbSet<Ge35mginspect> Ge35mginspects { get; set; }

    public virtual DbSet<Ge35odinspect> Ge35odinspects { get; set; }

    public virtual DbSet<Ge35rfinspect> Ge35rfinspects { get; set; }

    public virtual DbSet<Ge35sninspect> Ge35sninspects { get; set; }

    public virtual DbSet<Ge36bdinspect> Ge36bdinspects { get; set; }

    public virtual DbSet<Ge36cainspect> Ge36cainspects { get; set; }

    public virtual DbSet<Ge36cfinspect> Ge36cfinspects { get; set; }

    public virtual DbSet<Ge36clinspect> Ge36clinspects { get; set; }

    public virtual DbSet<Ge36deinspect> Ge36deinspects { get; set; }

    public virtual DbSet<Ge36ecinspect> Ge36ecinspects { get; set; }

    public virtual DbSet<Ge36edinspect> Ge36edinspects { get; set; }

    public virtual DbSet<Ge36finalPart> Ge36finalParts { get; set; }

    public virtual DbSet<Ge36flinspect> Ge36flinspects { get; set; }

    public virtual DbSet<Ge36inspect> Ge36inspects { get; set; }

    public virtual DbSet<Ge36mginspect> Ge36mginspects { get; set; }

    public virtual DbSet<Ge36rfinspect> Ge36rfinspects { get; set; }

    public virtual DbSet<Ge36sninspect> Ge36sninspects { get; set; }

    public virtual DbSet<Gm34bdinspect> Gm34bdinspects { get; set; }

    public virtual DbSet<Gm34blinspect> Gm34blinspects { get; set; }

    public virtual DbSet<Gm34bsinspect> Gm34bsinspects { get; set; }

    public virtual DbSet<Gm34cainspect> Gm34cainspects { get; set; }

    public virtual DbSet<Gm34cbinspect> Gm34cbinspects { get; set; }

    public virtual DbSet<Gm34cfinspect> Gm34cfinspects { get; set; }

    public virtual DbSet<Gm34clinspect> Gm34clinspects { get; set; }

    public virtual DbSet<Gm34deinspect> Gm34deinspects { get; set; }

    public virtual DbSet<Gm34edinspect> Gm34edinspects { get; set; }

    public virtual DbSet<Gm34elinspect> Gm34elinspects { get; set; }

    public virtual DbSet<Gm34finalPart> Gm34finalParts { get; set; }

    public virtual DbSet<Gm34flinspect> Gm34flinspects { get; set; }

    public virtual DbSet<Gm34inspect> Gm34inspects { get; set; }

    public virtual DbSet<Gm34lminspect> Gm34lminspects { get; set; }

    public virtual DbSet<Gm34mpinspect> Gm34mpinspects { get; set; }

    public virtual DbSet<Gm34rfinspect> Gm34rfinspects { get; set; }

    public virtual DbSet<Gm34sninspect> Gm34sninspects { get; set; }

    public virtual DbSet<Gm34trinspect> Gm34trinspects { get; set; }

    public virtual DbSet<Gm35blinspect> Gm35blinspects { get; set; }

    public virtual DbSet<Gm35bsinspect> Gm35bsinspects { get; set; }

    public virtual DbSet<Gm35cainspect> Gm35cainspects { get; set; }

    public virtual DbSet<Gm35cbinspect> Gm35cbinspects { get; set; }

    public virtual DbSet<Gm35cfinspect> Gm35cfinspects { get; set; }

    public virtual DbSet<Gm35clinspect> Gm35clinspects { get; set; }

    public virtual DbSet<Gm35deinspect> Gm35deinspects { get; set; }

    public virtual DbSet<Gm35edinspect> Gm35edinspects { get; set; }

    public virtual DbSet<Gm35elinspect> Gm35elinspects { get; set; }

    public virtual DbSet<Gm35finalPart> Gm35finalParts { get; set; }

    public virtual DbSet<Gm35flinspect> Gm35flinspects { get; set; }

    public virtual DbSet<Gm35inspect> Gm35inspects { get; set; }

    public virtual DbSet<Gm35lminspect> Gm35lminspects { get; set; }

    public virtual DbSet<Gm35mpinspect> Gm35mpinspects { get; set; }

    public virtual DbSet<Gm35rfinspect> Gm35rfinspects { get; set; }

    public virtual DbSet<Gm35sninspect> Gm35sninspects { get; set; }

    public virtual DbSet<Gm35trinspect> Gm35trinspects { get; set; }

    public virtual DbSet<Gm35wainspect> Gm35wainspects { get; set; }

    public virtual DbSet<Gm36bpinspect> Gm36bpinspects { get; set; }

    public virtual DbSet<Gm36bsinspect> Gm36bsinspects { get; set; }

    public virtual DbSet<Gm36bvinspect> Gm36bvinspects { get; set; }

    public virtual DbSet<Gm36cainspect> Gm36cainspects { get; set; }

    public virtual DbSet<Gm36cbinspect> Gm36cbinspects { get; set; }

    public virtual DbSet<Gm36cfinspect> Gm36cfinspects { get; set; }

    public virtual DbSet<Gm36clinspect> Gm36clinspects { get; set; }

    public virtual DbSet<Gm36deinspect> Gm36deinspects { get; set; }

    public virtual DbSet<Gm36ecinspect> Gm36ecinspects { get; set; }

    public virtual DbSet<Gm36edinspect> Gm36edinspects { get; set; }

    public virtual DbSet<Gm36elinspect> Gm36elinspects { get; set; }

    public virtual DbSet<Gm36finalPart> Gm36finalParts { get; set; }

    public virtual DbSet<Gm36flinspect> Gm36flinspects { get; set; }

    public virtual DbSet<Gm36inspect> Gm36inspects { get; set; }

    public virtual DbSet<Gm36lcinspect> Gm36lcinspects { get; set; }

    public virtual DbSet<Gm36lminspect> Gm36lminspects { get; set; }

    public virtual DbSet<Gm36rfinspect> Gm36rfinspects { get; set; }

    public virtual DbSet<Gm36sninspect> Gm36sninspects { get; set; }

    public virtual DbSet<Gm36trinspect> Gm36trinspects { get; set; }

    public virtual DbSet<Gm36wainspect> Gm36wainspects { get; set; }

    public virtual DbSet<InfoLoco> InfoLocos { get; set; }

    public virtual DbSet<InfoLocosFinal> InfoLocosFinals { get; set; }

    public virtual DbSet<InspectionWarnInfo> InspectionWarnInfos { get; set; }

    public virtual DbSet<InternalFinalPart> InternalFinalParts { get; set; }

    public virtual DbSet<LeaseCoUser> LeaseCoUsers { get; set; }

    public virtual DbSet<LeftMidDoorInspect> LeftMidDoorInspects { get; set; }

    public virtual DbSet<LocoDashboard> LocoDashboards { get; set; }

    public virtual DbSet<LocoInfoCapture> LocoInfoCaptures { get; set; }

    public virtual DbSet<LocoInput> LocoInputs { get; set; }

    public virtual DbSet<ManualDcfinput> ManualDcfinputs { get; set; }

    public virtual DbSet<MarketValueLoco> MarketValueLocos { get; set; }

    public virtual DbSet<MarketValueWagon> MarketValueWagons { get; set; }

    public virtual DbSet<MarketWagon> MarketWagons { get; set; }

    public virtual DbSet<MasterLoco> MasterLocos { get; set; }

    public virtual DbSet<MasterWagon> MasterWagons { get; set; }

    public virtual DbSet<MergedGm35New> MergedGm35News { get; set; }

    public virtual DbSet<MergedGm36New> MergedGm36News { get; set; }

    public virtual DbSet<MergedPartsGm34New> MergedPartsGm34News { get; set; }

    public virtual DbSet<MergedSheetsE18> MergedSheetsE18s { get; set; }

    public virtual DbSet<MergedSheetsGe34> MergedSheetsGe34s { get; set; }

    public virtual DbSet<MergedSheetsGe35> MergedSheetsGe35s { get; set; }

    public virtual DbSet<MergedSheetsGe36> MergedSheetsGe36s { get; set; }

    public virtual DbSet<MergedSheetsGe36New> MergedSheetsGe36News { get; set; }

    public virtual DbSet<MidPanInspect> MidPanInspects { get; set; }

    public virtual DbSet<RoofInspect> RoofInspects { get; set; }

    public virtual DbSet<ShortNoseInspect> ShortNoseInspects { get; set; }

    public virtual DbSet<StanchionsInspect> StanchionsInspects { get; set; }

    public virtual DbSet<TankersInspect> TankersInspects { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamInspector> TeamInspectors { get; set; }

    public virtual DbSet<TopRightPanInspect> TopRightPanInspects { get; set; }

    public virtual DbSet<TwistlocksInspect> TwistlocksInspects { get; set; }

    public virtual DbSet<VacBrakeFinalPart> VacBrakeFinalParts { get; set; }

    public virtual DbSet<VacBrakePartsInspect> VacBrakePartsInspects { get; set; }

    public virtual DbSet<WaccSetup> WaccSetups { get; set; }

    public virtual DbSet<WagonDashboard> WagonDashboards { get; set; }

    public virtual DbSet<WagonDashboardUploaded> WagonDashboardUploadeds { get; set; }

    public virtual DbSet<WagonFinalPart> WagonFinalParts { get; set; }

    public virtual DbSet<WagonGroup> WagonGroups { get; set; }

    public virtual DbSet<WagonInfoCapture> WagonInfoCaptures { get; set; }

    public virtual DbSet<WagonInput> WagonInputs { get; set; }

    public virtual DbSet<WagonPartsInspect> WagonPartsInspects { get; set; }

    public virtual DbSet<WalkAroundInspect> WalkAroundInspects { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AirBrakeFinalPart>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<AirBrakePartsInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AirBrake__3214EC27FBB6D888");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AssetTypeSetup>(entity =>
        {
            entity.HasKey(e => e.AssetType).HasName("PK__AssetTyp__7F6321AB361EB453");

            entity.ToTable("AssetTypeSetup");

            entity.Property(e => e.AssetType)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.DateSaved)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LeaseIncome)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishmentCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SavedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BatSwitchInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('BS',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_BatSwitch])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.BatSwitchInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BatSwitch");

            entity.HasOne(d => d.User).WithMany(p => p.BatSwitchInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BatSwitchInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<BotLeftPanInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('BL',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_BotLeftPan])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.BotLeftPanInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BotLeftPan");

            entity.HasOne(d => d.User).WithMany(p => p.BotLeftPanInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BotLeftPanInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<BottomDischargeInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BottomDi__3214EC27E686597E");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CabLocoInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('CL',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_CabLoco])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.CabLocoInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CabLoco");

            entity.HasOne(d => d.User).WithMany(p => p.CabLocoInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CabLocoInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<CenAirInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('CA',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_CenAir])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.CenAirInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CenAir");

            entity.HasOne(d => d.User).WithMany(p => p.CenAirInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CenAirInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<CirBreakPanInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('CB',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_CirBreakPan])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.CirBreakPanInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CirBreakPan");

            entity.HasOne(d => d.User).WithMany(p => p.CirBreakPanInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CirBreakPanInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<CockpitAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId).HasName("PK__CockpitA__B3C6D64B1F90214B");

            entity.ToTable("CockpitAllocation");

            entity.Property(e => e.AssetType).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RefNo)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CockpitGlobalConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CockpitG__3214EC07122F9108");

            entity.ToTable("CockpitGlobalConfig");

            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<ComFanInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('CF',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_ComFan])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.ComFanInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComFan");

            entity.HasOne(d => d.User).WithMany(p => p.ComFanInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComFanInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<ConditionRating>(entity =>
        {
            entity.HasKey(e => e.Score);

            entity.ToTable("ConditionRating");

            entity.Property(e => e.Score).ValueGeneratedNever();
            entity.Property(e => e.Condition)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.OperationalStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CoupGearInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('CG',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_CoupGear])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.CoupGearInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoupGear");

            entity.HasOne(d => d.User).WithMany(p => p.CoupGearInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoupGearInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<D34loco>(entity =>
        {
            entity.HasKey(e => e.Num).HasName("PK__D34Locos__C7D08B6334898064");

            entity.ToTable("D34Locos");

            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);

            entity.HasOne(d => d.AssetCodeNavigation).WithMany(p => p.D34locos)
                .HasForeignKey(d => d.AssetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_D34Locos");
        });

        modelBuilder.Entity<D34part>(entity =>
        {
            entity.ToTable("D34Parts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<D35loco>(entity =>
        {
            entity.HasKey(e => e.Num).HasName("PK__D35Locos__C7D08B63A80EE940");

            entity.ToTable("D35Locos");

            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);

            entity.HasOne(d => d.AssetCodeNavigation).WithMany(p => p.D35locos)
                .HasForeignKey(d => d.AssetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_D35Locos");
        });

        modelBuilder.Entity<D35part>(entity =>
        {
            entity.ToTable("D35Parts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<D36loco>(entity =>
        {
            entity.HasKey(e => e.Num).HasName("PK__D36Locos__C7D08B6344B1805F");

            entity.ToTable("D36Locos");

            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);

            entity.HasOne(d => d.AssetCodeNavigation).WithMany(p => p.D36locos)
                .HasForeignKey(d => d.AssetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_D36Locos");
        });

        modelBuilder.Entity<DashBoardItem>(entity =>
        {
            entity.HasKey(e => e.Record);

            entity.Property(e => e.Record)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('REC',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_Roof])),(4))))");
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BodyRepairValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.InspectorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LiftingRequired)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ProMain)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UploadStatus)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.DashBoardItems)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DashBoardItems");
        });

        modelBuilder.Entity<DeviceLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DeviceLo__3214EC07048686D5");

            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.DeviceTimestamp).HasColumnType("datetime");
            entity.Property(e => e.ServerTimestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UserName).HasMaxLength(50);
        });

        modelBuilder.Entity<DoorsInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DoorsIns__3214EC2777278499");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<E18bdinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18BDIns__3214EC273DA08F33");

            entity.ToTable("E18BDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18beinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18BEIns__3214EC27A8C25C04");

            entity.ToTable("E18BEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18ccinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18CCIns__3214EC2703B5748D");

            entity.ToTable("E18CCInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18crinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18CRIns__3214EC27848599FA");

            entity.ToTable("E18CRInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18ctinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18CTIns__3214EC2720F93369");

            entity.ToTable("E18CTInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18eeinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18EEIns__3214EC27149D1CC2");

            entity.ToTable("E18EEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18ehinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18EHIns__3214EC27A2120E19");

            entity.ToTable("E18EHInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18esinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18ESIns__3214EC27BF1782C8");

            entity.ToTable("E18ESInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18finalPart>(entity =>
        {
            entity.ToTable("E18FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18FLIns__3214EC271DCB24FA");

            entity.ToTable("E18FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18hcinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18HCIns__3214EC27501C7B27");

            entity.ToTable("E18HCInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18hsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18HSIns__3214EC27D6078F3A");

            entity.ToTable("E18HSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18hvinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18HVIns__3214EC27D1D2E00F");

            entity.ToTable("E18HVInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18Inspe__3214EC2722C28923");

            entity.ToTable("E18Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.E18inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_E18_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.E18inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_E18_User");
        });

        modelBuilder.Entity<E18loco>(entity =>
        {
            entity.HasKey(e => e.Num).HasName("PK__E18Locos__C7D08B6303784688");

            entity.ToTable("E18Locos");

            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);

            entity.HasOne(d => d.AssetCodeNavigation).WithMany(p => p.E18locos)
                .HasForeignKey(d => d.AssetCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_E18Locos");
        });

        modelBuilder.Entity<E18lvinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18LVIns__3214EC2787A0C04C");

            entity.ToTable("E18LVInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18mainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18MAIns__3214EC277AEA718E");

            entity.ToTable("E18MAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18mbinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18MBIns__3214EC271DB78F2F");

            entity.ToTable("E18MBInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<E18rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__E18RFIns__3214EC278B54A3B9");

            entity.ToTable("E18RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<ElectCabInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('EC',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_ElectCab])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.ElectCabInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ElectCab");

            entity.HasOne(d => d.User).WithMany(p => p.ElectCabInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ElectCabInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<EndDeckInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('DE',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_EndDeck])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.EndDeckInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EndDeck");

            entity.HasOne(d => d.User).WithMany(p => p.EndDeckInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EndDeckInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<EngineDeckInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('ED',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_EngineDeck])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.EngineDeckInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EngineDeck");

            entity.HasOne(d => d.User).WithMany(p => p.EngineDeckInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EngineDeckInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<FloorInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FloorIns__3214EC27B3B41DE4");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<FrontLocoInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('FR',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_FrontLoco])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.FrontLocoInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FrontLoco");

            entity.HasOne(d => d.User).WithMany(p => p.FrontLocoInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FrontLocoInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<Ge34acinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34ACIn__3214EC2743550C98");

            entity.ToTable("GE34ACInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34bcinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34BCIn__3214EC2794497E87");

            entity.ToTable("GE34BCInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34bdinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34BDIn__3214EC276642D762");

            entity.ToTable("GE34BDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34bsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34BSIn__3214EC2775AB43F6");

            entity.ToTable("GE34BSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34CFIn__3214EC27EDC42946");

            entity.ToTable("GE34CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34CLIn__3214EC2729E531CE");

            entity.ToTable("GE34CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34DEIn__3214EC27B75DA56E");

            entity.ToTable("GE34DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34ecinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34ECIn__3214EC2761F913B7");

            entity.ToTable("GE34ECInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34EDIn__3214EC273742739A");

            entity.ToTable("GE34EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34finalPart>(entity =>
        {
            entity.ToTable("GE34FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34FLIn__3214EC272A742900");

            entity.ToTable("GE34FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34Insp__3214EC272DD278C5");

            entity.ToTable("GE34Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Ge34inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE34_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Ge34inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE34_User");
        });

        modelBuilder.Entity<Ge34odinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34ODIn__3214EC278B7AC38F");

            entity.ToTable("GE34ODInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34RFIn__3214EC27FABA2746");

            entity.ToTable("GE34RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge34sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE34SNIn__3214EC274C48C78A");

            entity.ToTable("GE34SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35bcinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35BCIn__3214EC27A1DD85EE");

            entity.ToTable("GE35BCInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35bdinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35BDIn__3214EC274DD1FEC9");

            entity.ToTable("GE35BDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35bsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35BSIn__3214EC27E1A26DB4");

            entity.ToTable("GE35BSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35CFIn__3214EC27750458DC");

            entity.ToTable("GE35CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35CLIn__3214EC2769582092");

            entity.ToTable("GE35CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35DEIn__3214EC274B911152");

            entity.ToTable("GE35DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35ecinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35ECIn__3214EC276D4D7277");

            entity.ToTable("GE35ECInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35EDIn__3214EC27670D6534");

            entity.ToTable("GE35EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35finalPart>(entity =>
        {
            entity.ToTable("GE35FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35FLIn__3214EC27062E76DD");

            entity.ToTable("GE35FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35Insp__3214EC27940E0368");

            entity.ToTable("GE35Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Ge35inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE35_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Ge35inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE35_User");
        });

        modelBuilder.Entity<Ge35mginspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35MGIn__3214EC2760572189");

            entity.ToTable("GE35MGInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35odinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35ODIn__3214EC2734144563");

            entity.ToTable("GE35ODInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35RFIn__3214EC2761701292");

            entity.ToTable("GE35RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge35sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE35SNIn__3214EC27729B4562");

            entity.ToTable("GE35SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36bdinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36BDIn__3214EC2787F580F3");

            entity.ToTable("GE36BDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36cainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36CAIn__3214EC2744F1F0B0");

            entity.ToTable("GE36CAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36CFIn__3214EC27D8E0F1FE");

            entity.ToTable("GE36CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36CLIn__3214EC27E5526E84");

            entity.ToTable("GE36CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36DEIn__3214EC27910870BF");

            entity.ToTable("GE36DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36ecinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36ECIn__3214EC27A695ECD4");

            entity.ToTable("GE36ECInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36EDIn__3214EC275450315B");

            entity.ToTable("GE36EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36finalPart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_D36FinalParts");

            entity.ToTable("GE36FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36FLIn__3214EC272E1179CA");

            entity.ToTable("GE36FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36Insp__3214EC275A368920");

            entity.ToTable("GE36Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Ge36inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE36_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Ge36inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GE36_User");
        });

        modelBuilder.Entity<Ge36mginspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36MGIn__3214EC278CACEAF8");

            entity.ToTable("GE36MGInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36RFIn__3214EC279F50E7B8");

            entity.ToTable("GE36RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Ge36sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GE36SNIn__3214EC2733FE329C");

            entity.ToTable("GE36SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34bdinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34BDIn__3214EC27D735BC98");

            entity.ToTable("GM34BDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34blinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34BLIn__3214EC2746B42F11");

            entity.ToTable("GM34BLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34bsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34BSIn__3214EC2760C0A13B");

            entity.ToTable("GM34BSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34cainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34CAIn__3214EC27F0800DCE");

            entity.ToTable("GM34CAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34cbinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34CBIn__3214EC27E302F701");

            entity.ToTable("GM34CBInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34CFIn__3214EC27D21262CC");

            entity.ToTable("GM34CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34CLIn__3214EC27AFAD3B3D");

            entity.ToTable("GM34CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34DEIn__3214EC279EDA5F14");

            entity.ToTable("GM34DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34EDIn__3214EC27B720DCAA");

            entity.ToTable("GM34EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34elinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34ELIn__3214EC27FCD3F0BF");

            entity.ToTable("GM34ELInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34finalPart>(entity =>
        {
            entity.ToTable("GM34FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34FLIn__3214EC27575EF6A6");

            entity.ToTable("GM34FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34Insp__3214EC27E3F0D4A2");

            entity.ToTable("GM34Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Gm34inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM34_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Gm34inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM34_User");
        });

        modelBuilder.Entity<Gm34lminspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34LMIn__3214EC2749AC901F");

            entity.ToTable("GM34LMInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34mpinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34MPIn__3214EC2754C4480F");

            entity.ToTable("GM34MPInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34RFIn__3214EC278FC1BC4D");

            entity.ToTable("GM34RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34SNIn__3214EC276773FB43");

            entity.ToTable("GM34SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm34trinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM34TRIn__3214EC27AF8E8A91");

            entity.ToTable("GM34TRInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35blinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35BLIn__3214EC27BC255F9D");

            entity.ToTable("GM35BLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35bsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35BSIn__3214EC27737E78CD");

            entity.ToTable("GM35BSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35cainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35CAIn__3214EC27D94CE393");

            entity.ToTable("GM35CAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35cbinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35CBIn__3214EC278811D7F2");

            entity.ToTable("GM35CBInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35CFIn__3214EC2781D46C58");

            entity.ToTable("GM35CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35CLIn__3214EC275A6C0145");

            entity.ToTable("GM35CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35DEIn__3214EC27C78B2990");

            entity.ToTable("GM35DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35EDIn__3214EC272BD075BC");

            entity.ToTable("GM35EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35elinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35ELIn__3214EC27A3A29251");

            entity.ToTable("GM35ELInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35finalPart>(entity =>
        {
            entity.ToTable("GM35FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35FLIn__3214EC270BFA6A7B");

            entity.ToTable("GM35FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35Insp__3214EC271D435B11");

            entity.ToTable("GM35Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Gm35inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM35_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Gm35inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM35_User");
        });

        modelBuilder.Entity<Gm35lminspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35LMIn__3214EC279F06B783");

            entity.ToTable("GM35LMInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35mpinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35MPIn__3214EC2716EC08CE");

            entity.ToTable("GM35MPInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35RFIn__3214EC279F2BA3A2");

            entity.ToTable("GM35RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35SNIn__3214EC27049AF422");

            entity.ToTable("GM35SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35trinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35TRIn__3214EC27F1E3124A");

            entity.ToTable("GM35TRInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm35wainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM35WAIn__3214EC27236CFDB6");

            entity.ToTable("GM35WAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36bpinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36BPIn__3214EC2799CAC7F8");

            entity.ToTable("GM36BPInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36bsinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36BSIn__3214EC272CFA249E");

            entity.ToTable("GM36BSInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36bvinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36BVIn__3214EC275183B5A0");

            entity.ToTable("GM36BVInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36cainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36CAIn__3214EC27770E48EC");

            entity.ToTable("GM36CAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36cbinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36CBIn__3214EC27C1277FA9");

            entity.ToTable("GM36CBInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36cfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36CFIn__3214EC27004C876F");

            entity.ToTable("GM36CFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36clinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36CLIn__3214EC27FFA189AA");

            entity.ToTable("GM36CLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36deinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36DEIn__3214EC27406761B2");

            entity.ToTable("GM36DEInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36ecinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36ECIn__3214EC276BD49E2E");

            entity.ToTable("GM36ECInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36edinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36EDIn__3214EC27E6F99C2B");

            entity.ToTable("GM36EDInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36elinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36ELIn__3214EC27255A14A1");

            entity.ToTable("GM36ELInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36finalPart>(entity =>
        {
            entity.ToTable("GM36FinalParts");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LabourValue).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36flinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36FLIn__3214EC275B1A8D94");

            entity.ToTable("GM36FLInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36inspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36Insp__3214EC2786F7E1BD");

            entity.ToTable("GM36Inspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.Gm36inspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM36_LocoNumber");

            entity.HasOne(d => d.User).WithMany(p => p.Gm36inspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GM36_User");
        });

        modelBuilder.Entity<Gm36lcinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36LCIn__3214EC277F104086");

            entity.ToTable("GM36LCInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36lminspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36LMIn__3214EC2792B75C6E");

            entity.ToTable("GM36LMInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36rfinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36RFIn__3214EC278D719077");

            entity.ToTable("GM36RFInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36sninspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36SNIn__3214EC27E624D92E");

            entity.ToTable("GM36SNInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36trinspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36TRIn__3214EC27B81475A8");

            entity.ToTable("GM36TRInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<Gm36wainspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GM36WAIn__3214EC270C324E86");

            entity.ToTable("GM36WAInspects");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<InfoLoco>(entity =>
        {
            entity.HasKey(e => e.InfoId);

            entity.Property(e => e.InfoId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('INFO',right(concat('00',CONVERT([varchar](3),NEXT VALUE FOR [dbo].[Seq_InfoLoco])),(3))))")
                .HasColumnName("InfoID");
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BodyRepairValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FleetRenewPro)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LiftingRequired)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProMain)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.InfoLocos)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InfoLocos_MasterLocos");
        });

        modelBuilder.Entity<InfoLocosFinal>(entity =>
        {
            entity.HasKey(e => e.InfoId);

            entity.ToTable("InfoLocosFinal");

            entity.Property(e => e.InfoId)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('INFO',right(concat('00',CONVERT([varchar](3),NEXT VALUE FOR [dbo].[Seq_InfoLocoFinal])),(3))))");
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BodyRepairValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FleetRenewPro)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LiftingRequired)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProMain)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.InfoLocosFinals)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InfoLocosFinal_MasterLocos");
        });

        modelBuilder.Entity<InspectionWarnInfo>(entity =>
        {
            entity.ToTable("InspectionWarnInfo");

            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InspectionNumber).HasMaxLength(50);
            entity.Property(e => e.InspectionType).HasMaxLength(50);
            entity.Property(e => e.Lat).HasMaxLength(50);
            entity.Property(e => e.Long).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<InternalFinalPart>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<LeaseCoUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.UserEmail, "UQ_LeaseCoUsers_UserEmail").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ_LeaseCoUsers_UserName").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('U',right(concat('00',CONVERT([varchar](3),NEXT VALUE FOR [dbo].[Seq_LeaseCoUser])),(3))))")
                .HasColumnName("UserID");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UserEmail)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserPassword).HasMaxLength(300);
            entity.Property(e => e.UserRole)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<LeftMidDoorInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('LM',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_LeftMidDoor])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.LeftMidDoorInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeftMidDoor");

            entity.HasOne(d => d.User).WithMany(p => p.LeftMidDoorInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeftMidDoorInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<LocoDashboard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LocoDash__3214EC07A7F274C2");

            entity.ToTable("LocoDashboard");

            entity.Property(e => e.AssetValue).HasMaxLength(100);
            entity.Property(e => e.BodyDamage).HasMaxLength(10);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.DateAssessed).HasMaxLength(20);
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InspectorId).HasMaxLength(100);
            entity.Property(e => e.InspectorName).HasMaxLength(150);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel).HasMaxLength(50);
            entity.Property(e => e.MarketValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(20);
            entity.Property(e => e.OperationalStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(20);
            entity.Property(e => e.ReplaceValue).HasMaxLength(20);
            entity.Property(e => e.ReplacementValue).HasMaxLength(100);
            entity.Property(e => e.StartTimeInspect).HasMaxLength(50);
            entity.Property(e => e.TimeAssessed).HasMaxLength(20);
            entity.Property(e => e.TotalLaborValue).HasMaxLength(100);
            entity.Property(e => e.TotalValue).HasMaxLength(100);
            entity.Property(e => e.UploadDate).HasMaxLength(20);
            entity.Property(e => e.UploadStatus).HasMaxLength(100);
        });

        modelBuilder.Entity<LocoInfoCapture>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LocoInfo__3214EC27532F3CA2");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.LiftDate).HasMaxLength(50);
            entity.Property(e => e.LocoClass)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(300);
        });

        modelBuilder.Entity<LocoInput>(entity =>
        {
            entity.HasKey(e => e.LocoNumber).HasName("PK__LocoInpu__9083114F094339F0");

            entity.Property(e => e.LocoNumber).ValueGeneratedNever();
            entity.Property(e => e.CorporateTaxRate).HasMaxLength(30);
            entity.Property(e => e.DateSaved).HasMaxLength(50);
            entity.Property(e => e.EscalationRate).HasMaxLength(30);
            entity.Property(e => e.LeaseIncome).HasMaxLength(100);
            entity.Property(e => e.LocoType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);
            entity.Property(e => e.OperatingCosts).HasMaxLength(100);
            entity.Property(e => e.OperatingCostsEscalation).HasMaxLength(30);
            entity.Property(e => e.PostTax).HasMaxLength(30);
            entity.Property(e => e.PreTax).HasMaxLength(30);
            entity.Property(e => e.RefurbishmentCost).HasMaxLength(100);
            entity.Property(e => e.ResidualValue).HasMaxLength(100);
            entity.Property(e => e.SavedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ScrapValue).HasMaxLength(100);
            entity.Property(e => e.ScrappingCost).HasMaxLength(100);
        });

        modelBuilder.Entity<ManualDcfinput>(entity =>
        {
            entity.ToTable("ManualDCFInput");

            entity.Property(e => e.AssetNumber).HasColumnType("numeric(18, 0)");
            entity.Property(e => e.AssetType).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.RefurbishValue).HasMaxLength(50);
            entity.Property(e => e.ScrapValue).HasMaxLength(50);
            entity.Property(e => e.TransferValue).HasMaxLength(50);
        });

        modelBuilder.Entity<MarketValueLoco>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.MarketValue)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MarketValueWagon>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.MarketValue)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MarketWagon>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.MarketValue)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MasterLoco>(entity =>
        {
            entity.HasKey(e => e.LocoNumber);

            entity.Property(e => e.LocoNumber).ValueGeneratedNever();
            entity.Property(e => e.AcqusitionValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.AvailNov24).HasMaxLength(4);
            entity.Property(e => e.CompareLocosAgainst695List).HasMaxLength(50);
            entity.Property(e => e.ConditionNov24).HasMaxLength(150);
            entity.Property(e => e.ConditionsSummary).HasMaxLength(100);
            entity.Property(e => e.Corridors).HasMaxLength(100);
            entity.Property(e => e.Descrip).HasMaxLength(100);
            entity.Property(e => e.Dest).HasMaxLength(50);
            entity.Property(e => e.Home).HasMaxLength(100);
            entity.Property(e => e.HoursS).HasMaxLength(50);
            entity.Property(e => e.HoursT).HasMaxLength(50);
            entity.Property(e => e.InspectedBy).HasMaxLength(50);
            entity.Property(e => e.InspectedDate).HasColumnType("datetime");
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoModel)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.Lstat)
                .HasMaxLength(50)
                .HasColumnName("LStat");
            entity.Property(e => e.MarketValue).HasMaxLength(100);
            entity.Property(e => e.Move).HasMaxLength(50);
            entity.Property(e => e.NetBookValue)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NextScreen).HasMaxLength(50);
            entity.Property(e => e.Org).HasMaxLength(50);
            entity.Property(e => e.Rdate).HasColumnName("RDate");
            entity.Property(e => e.Remark).HasMaxLength(100);
            entity.Property(e => e.RepairC).HasMaxLength(100);
            entity.Property(e => e.ReturnToServiceCostAsIs).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ScrapValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Sdate).HasColumnName("SDate");
            entity.Property(e => e.Sdep)
                .HasMaxLength(100)
                .HasColumnName("SDep");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Stype)
                .HasMaxLength(50)
                .HasColumnName("SType");
            entity.Property(e => e.TimeToRtsmonths).HasColumnName("TimeToRTSMonths");
            entity.Property(e => e.WorkScopeToRtsimmediately)
                .HasMaxLength(200)
                .HasColumnName("WorkScopeToRTSImmediately");
            entity.Property(e => e._619).HasMaxLength(50);
        });

        modelBuilder.Entity<MasterWagon>(entity =>
        {
            entity.HasKey(e => e.WagonNumber);

            entity.Property(e => e.WagonNumber).ValueGeneratedNever();
            entity.Property(e => e.AcqusitionValue).HasMaxLength(100);
            entity.Property(e => e.ActiveStatus).HasMaxLength(50);
            entity.Property(e => e.Age).HasMaxLength(50);
            entity.Property(e => e.ArrivedOrEnroute).HasMaxLength(50);
            entity.Property(e => e.AssetRegister).HasMaxLength(50);
            entity.Property(e => e.AssetVerificationLocation).HasMaxLength(50);
            entity.Property(e => e.AssetVerificationStatus).HasMaxLength(50);
            entity.Property(e => e.AssignedTo).HasMaxLength(50);
            entity.Property(e => e.BrakeSystem).HasMaxLength(50);
            entity.Property(e => e.CommodityAssignedToTheWagons).HasMaxLength(50);
            entity.Property(e => e.ConditionRating).HasMaxLength(50);
            entity.Property(e => e.ContentsCode).HasMaxLength(50);
            entity.Property(e => e.CorridorName).HasMaxLength(50);
            entity.Property(e => e.CurrentLocationDesc).HasMaxLength(50);
            entity.Property(e => e.Decision).HasMaxLength(50);
            entity.Property(e => e.DeemedActiveStandingTimePenaltyCost)
                .HasMaxLength(100)
                .HasColumnName("DeemedActiveStandingTimePenalty_Cost");
            entity.Property(e => e.DepartureDate).HasMaxLength(50);
            entity.Property(e => e.DepotSchedCost).HasMaxLength(100);
            entity.Property(e => e.DepotUnschedCost).HasMaxLength(100);
            entity.Property(e => e.FinalDestDesc).HasMaxLength(50);
            entity.Property(e => e.Fleet).HasMaxLength(50);
            entity.Property(e => e.Gauge).HasMaxLength(50);
            entity.Property(e => e.HsbogiesFitted)
                .HasMaxLength(50)
                .HasColumnName("HSBogiesFitted");
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.IsApressureVessel).HasColumnName("IsAPressureVessel");
            entity.Property(e => e.LiftingCycle).HasMaxLength(50);
            entity.Property(e => e.LoadPlaceDesc).HasMaxLength(50);
            entity.Property(e => e.LoadedOrEmptyOrScrap).HasMaxLength(50);
            entity.Property(e => e.MarketValue).HasMaxLength(100);
            entity.Property(e => e.Mis)
                .HasMaxLength(50)
                .HasColumnName("MIS");
            entity.Property(e => e.NetBookValue).HasMaxLength(100);
            entity.Property(e => e.NextBarrelTest).HasMaxLength(50);
            entity.Property(e => e.NextLifting).HasMaxLength(50);
            entity.Property(e => e.NextLiftingAfterReturningToService).HasMaxLength(50);
            entity.Property(e => e.NotificationDescription).HasMaxLength(50);
            entity.Property(e => e.PrevContents).HasMaxLength(50);
            entity.Property(e => e.ReaderName).HasMaxLength(100);
            entity.Property(e => e.ReaderNumber).HasMaxLength(50);
            entity.Property(e => e.ReaderPlaceCode).HasMaxLength(50);
            entity.Property(e => e.ReaderReportStatus).HasMaxLength(100);
            entity.Property(e => e.ReservedFor).HasMaxLength(50);
            entity.Property(e => e.ReturnToServiceInterventionCategory).HasMaxLength(50);
            entity.Property(e => e.RevisedBrakeSystem).HasMaxLength(50);
            entity.Property(e => e.RevisedCommodity).HasMaxLength(50);
            entity.Property(e => e.RevisedRtscost)
                .HasMaxLength(100)
                .HasColumnName("RevisedRTSCost");
            entity.Property(e => e.RevisedYearOfRts).HasColumnName("RevisedYearOfRTS");
            entity.Property(e => e.ScrapValue).HasMaxLength(100);
            entity.Property(e => e.TotalReturnToServiceEstimatedCost).HasMaxLength(100);
            entity.Property(e => e.TotalRts)
                .HasMaxLength(100)
                .HasColumnName("TotalRTS");
            entity.Property(e => e.TrainNumber).HasMaxLength(50);
            entity.Property(e => e.TransitTime).HasMaxLength(50);
            entity.Property(e => e.UpdatedStatus2).HasMaxLength(50);
            entity.Property(e => e.UpdatedStatus3).HasMaxLength(50);
            entity.Property(e => e.WagonStatus).HasMaxLength(50);
            entity.Property(e => e.WagonType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WorkshopCost).HasMaxLength(100);
            entity.Property(e => e.YearOfRts).HasColumnName("YearOfRTS");
            entity.Property(e => e.YearsStanding).HasMaxLength(50);
            entity.Property(e => e._1).HasMaxLength(100);
            entity.Property(e => e._10).HasMaxLength(100);
            entity.Property(e => e._2).HasMaxLength(100);
            entity.Property(e => e._3).HasMaxLength(100);
            entity.Property(e => e._4).HasMaxLength(100);
            entity.Property(e => e._5).HasMaxLength(100);
            entity.Property(e => e._6).HasMaxLength(100);
            entity.Property(e => e._7).HasMaxLength(100);
            entity.Property(e => e._8).HasMaxLength(100);
            entity.Property(e => e._9).HasMaxLength(100);
        });

        modelBuilder.Entity<MergedGm35New>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("merged_GM35_New");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_Cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
            entity.Property(e => e.Sheet).HasColumnName("sheet");
        });

        modelBuilder.Entity<MergedGm36New>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("merged_GM36_New");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_Cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
            entity.Property(e => e.Sheet).HasColumnName("sheet");
        });

        modelBuilder.Entity<MergedPartsGm34New>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("merged_parts_GM34_New");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_Cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
            entity.Property(e => e.Sheet).HasColumnName("sheet");
        });

        modelBuilder.Entity<MergedSheetsE18>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MERGED_SHEETS_E18");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.LabourCost).HasMaxLength(50);
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
        });

        modelBuilder.Entity<MergedSheetsGe34>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MERGED_SHEETS_GE34");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_Cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
        });

        modelBuilder.Entity<MergedSheetsGe35>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MERGED_SHEETS_GE35");

            entity.Property(e => e.LabourCost).HasMaxLength(50);
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
        });

        modelBuilder.Entity<MergedSheetsGe36>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MERGED_SHEETS_GE36");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
        });

        modelBuilder.Entity<MergedSheetsGe36New>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MERGED_SHEETS_GE36_New");

            entity.Property(e => e.LabourCost)
                .HasMaxLength(50)
                .HasColumnName("Labour_Cost");
            entity.Property(e => e.PartDescription).HasColumnName("Part_Description");
        });

        modelBuilder.Entity<MidPanInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('MP',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_MidPan])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.MidPanInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MidPan");

            entity.HasOne(d => d.User).WithMany(p => p.MidPanInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MidPanInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<RoofInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('RF',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_Roof])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.RoofInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Roof");

            entity.HasOne(d => d.User).WithMany(p => p.RoofInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RoofInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<ShortNoseInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('SN',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_ShortNose])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.ShortNoseInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShortNose");

            entity.HasOne(d => d.User).WithMany(p => p.ShortNoseInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShortNoseInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<StanchionsInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Stanchio__3214EC276621156E");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TankersInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TankersI__3214EC27F56C1141");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__Teams__123AE7B979585803");

            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TeamName).HasMaxLength(100);
        });

        modelBuilder.Entity<TeamInspector>(entity =>
        {
            entity.HasKey(e => e.TeamInspectorId).HasName("PK__TeamInsp__644125C4C0756182");

            entity.Property(e => e.TeamInspectorId).HasColumnName("TeamInspectorID");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InspectorId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("InspectorID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");

            entity.HasOne(d => d.Inspector).WithMany(p => p.TeamInspectors)
                .HasForeignKey(d => d.InspectorId)
                .HasConstraintName("FK_TeamInspectors_Users");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamInspectors)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK_TeamInspectors_Teams");
        });

        modelBuilder.Entity<TopRightPanInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('TR',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_TopRightPan])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.TopRightPanInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TopRightPan");

            entity.HasOne(d => d.User).WithMany(p => p.TopRightPanInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TopRightPanInspects_LeaseCoUsers");
        });

        modelBuilder.Entity<TwistlocksInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Twistloc__3214EC275F0B0EE9");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VacBrakeFinalPart>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<VacBrakePartsInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VacBrake__3214EC2798A78EB9");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WaccSetup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WaccSetu__3214EC276E992290");

            entity.ToTable("WaccSetup");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.PostTax).HasMaxLength(30);
            entity.Property(e => e.PreTax).HasMaxLength(30);
            entity.Property(e => e.UpdateBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UpdateDate).HasMaxLength(50);
        });

        modelBuilder.Entity<WagonDashboard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WagonDas__3214EC27D940424E");

            entity.ToTable("WagonDashboard");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AssetValue).HasMaxLength(100);
            entity.Property(e => e.BarrelDate).HasMaxLength(50);
            entity.Property(e => e.BarrelLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BarrelValue).HasMaxLength(100);
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BrakeDate).HasMaxLength(50);
            entity.Property(e => e.BrakeLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.DateAssessed).HasMaxLength(50);
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InspectorId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("InspectorID");
            entity.Property(e => e.InspectorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LiftDate).HasMaxLength(50);
            entity.Property(e => e.LiftLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LiftValue).HasMaxLength(100);
            entity.Property(e => e.MarketValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.OperationalStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.StartTimeInspect).HasMaxLength(50);
            entity.Property(e => e.TimeAssessed).HasMaxLength(50);
            entity.Property(e => e.TotalLaborValue).HasMaxLength(100);
            entity.Property(e => e.TotalValue).HasMaxLength(100);
            entity.Property(e => e.UploadDate).HasMaxLength(50);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WagonDashboardUploaded>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WagonDas__3214EC27BDD17D09");

            entity.ToTable("WagonDashboardUploaded");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AssetValue).HasMaxLength(100);
            entity.Property(e => e.BarrelDate).HasMaxLength(50);
            entity.Property(e => e.BarrelLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BarrelValue).HasMaxLength(100);
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BrakeDate).HasMaxLength(50);
            entity.Property(e => e.BrakeLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.DateAssessed).HasMaxLength(50);
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InspectorId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("InspectorID");
            entity.Property(e => e.InspectorName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LiftDate).HasMaxLength(50);
            entity.Property(e => e.LiftLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LiftValue).HasMaxLength(100);
            entity.Property(e => e.MarketValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.OperationalStatus)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.StartTimeInspect).HasMaxLength(50);
            entity.Property(e => e.TimeAssessed).HasMaxLength(50);
            entity.Property(e => e.TotalLaborValue).HasMaxLength(100);
            entity.Property(e => e.TotalValue).HasMaxLength(100);
            entity.Property(e => e.UploadDate).HasMaxLength(50);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WagonFinalPart>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
        });

        modelBuilder.Entity<WagonGroup>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("ID");
            entity.Property(e => e.AirBrake)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Doors)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.DualBrake)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Group)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Stanchions)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Twistlocks)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Type)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.VacuumBrake)
                .HasMaxLength(4)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WagonInfoCapture>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WagonInf__3214EC27BF5369BF");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BarrelDate).HasMaxLength(50);
            entity.Property(e => e.BarrelLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BodyDamage)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BrakeDate).HasMaxLength(50);
            entity.Property(e => e.BrakeLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.BrakeType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.GpsLatitude).HasMaxLength(100);
            entity.Property(e => e.GpsLongitude).HasMaxLength(100);
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.LiftDate).HasMaxLength(50);
            entity.Property(e => e.LiftLapsed)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.NetBookValue).HasMaxLength(300);
            entity.Property(e => e.StartInspectTime).HasMaxLength(50);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(30)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WagonInput>(entity =>
        {
            entity.HasKey(e => e.WagonNumber).HasName("PK__WagonInp__14359E4552755701");

            entity.Property(e => e.WagonNumber).ValueGeneratedNever();
            entity.Property(e => e.CorporateTaxRate).HasMaxLength(30);
            entity.Property(e => e.DateSaved).HasMaxLength(50);
            entity.Property(e => e.EscalationRate).HasMaxLength(30);
            entity.Property(e => e.LeaseIncome).HasMaxLength(100);
            entity.Property(e => e.NetBookValue).HasMaxLength(100);
            entity.Property(e => e.OperatingCosts).HasMaxLength(100);
            entity.Property(e => e.OperatingCostsEscalation).HasMaxLength(30);
            entity.Property(e => e.PostTax).HasMaxLength(30);
            entity.Property(e => e.PreTax).HasMaxLength(30);
            entity.Property(e => e.RefurbishmentCost).HasMaxLength(100);
            entity.Property(e => e.ResidualValue).HasMaxLength(100);
            entity.Property(e => e.SavedBy)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ScrapValue).HasMaxLength(100);
            entity.Property(e => e.ScrappingCost).HasMaxLength(100);
            entity.Property(e => e.WagonType)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WagonPartsInspect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WagonPar__3214EC2762228DF7");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FormId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("FormID");
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.LaborValue).HasMaxLength(100);
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.MissingValue).HasMaxLength(100);
            entity.Property(e => e.PartDescr)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PartId)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("PartID");
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishValue).HasMaxLength(100);
            entity.Property(e => e.ReplaceCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceValue).HasMaxLength(100);
            entity.Property(e => e.WagonGroup)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.WagonType)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<WalkAroundInspect>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.Property(e => e.ItemId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasDefaultValueSql("(concat('AS',right(concat('0000',CONVERT([varchar](4),NEXT VALUE FOR [dbo].[Seq_WalkAround])),(4))))")
                .HasColumnName("ItemID");
            entity.Property(e => e.DamageCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.GoodCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.InspectFormId)
                .HasMaxLength(7)
                .IsUnicode(false)
                .HasColumnName("InspectFormID");
            entity.Property(e => e.MissingCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.PartDescr).IsUnicode(false);
            entity.Property(e => e.RefurbishCheck)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.RefurbishCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ReplaceCost)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UserId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("UserID");

            entity.HasOne(d => d.LocoNumberNavigation).WithMany(p => p.WalkAroundInspects)
                .HasForeignKey(d => d.LocoNumber)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalkLoco");

            entity.HasOne(d => d.User).WithMany(p => p.WalkAroundInspects)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WalkAroundInspects_LeaseCoUsers");
        });
        modelBuilder.HasSequence<int>("Seq_BatSwitch");
        modelBuilder.HasSequence<int>("Seq_BotLeftPan");
        modelBuilder.HasSequence<int>("Seq_CabLoco");
        modelBuilder.HasSequence<int>("Seq_CenAir");
        modelBuilder.HasSequence<int>("Seq_CirBreakPan");
        modelBuilder.HasSequence<int>("Seq_ComFan");
        modelBuilder.HasSequence<int>("Seq_CoupGear");
        modelBuilder.HasSequence<int>("Seq_DashBoard");
        modelBuilder.HasSequence<int>("Seq_ElectCab");
        modelBuilder.HasSequence<int>("Seq_EndDeck");
        modelBuilder.HasSequence<int>("Seq_EngineDeck");
        modelBuilder.HasSequence<int>("Seq_FrontLoco");
        modelBuilder.HasSequence<int>("Seq_InfoLoco");
        modelBuilder.HasSequence<int>("Seq_InfoLocoFinal");
        modelBuilder.HasSequence<int>("Seq_LeaseCoUser");
        modelBuilder.HasSequence<int>("Seq_LeftMidDoor");
        modelBuilder.HasSequence<int>("Seq_MidPan");
        modelBuilder.HasSequence<int>("Seq_Roof");
        modelBuilder.HasSequence<int>("Seq_ShortNose");
        modelBuilder.HasSequence<int>("Seq_TopRightPan");
        modelBuilder.HasSequence<int>("Seq_WalkAround");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
