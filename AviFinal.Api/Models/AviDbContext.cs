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

    public virtual DbSet<BatSwitchInspect> BatSwitchInspects { get; set; }

    public virtual DbSet<BotLeftPanInspect> BotLeftPanInspects { get; set; }

    public virtual DbSet<CabLocoInspect> CabLocoInspects { get; set; }

    public virtual DbSet<CenAirInspect> CenAirInspects { get; set; }

    public virtual DbSet<CirBreakPanInspect> CirBreakPanInspects { get; set; }

    public virtual DbSet<ComFanInspect> ComFanInspects { get; set; }

    public virtual DbSet<CoupGearInspect> CoupGearInspects { get; set; }

    public virtual DbSet<D34loco> D34locos { get; set; }

    public virtual DbSet<D34part> D34parts { get; set; }

    public virtual DbSet<D35loco> D35locos { get; set; }

    public virtual DbSet<D35part> D35parts { get; set; }

    public virtual DbSet<D36loco> D36locos { get; set; }

    public virtual DbSet<DashBoardItem> DashBoardItems { get; set; }

    public virtual DbSet<E18finalPart> E18finalParts { get; set; }

    public virtual DbSet<E18inspect> E18inspects { get; set; }

    public virtual DbSet<E18loco> E18locos { get; set; }

    public virtual DbSet<ElectCabInspect> ElectCabInspects { get; set; }

    public virtual DbSet<EndDeckInspect> EndDeckInspects { get; set; }

    public virtual DbSet<EngineDeckInspect> EngineDeckInspects { get; set; }

    public virtual DbSet<FrontLocoInspect> FrontLocoInspects { get; set; }

    public virtual DbSet<Ge34finalPart> Ge34finalParts { get; set; }

    public virtual DbSet<Ge34inspect> Ge34inspects { get; set; }

    public virtual DbSet<Ge35finalPart> Ge35finalParts { get; set; }

    public virtual DbSet<Ge35inspect> Ge35inspects { get; set; }

    public virtual DbSet<Ge36finalPart> Ge36finalParts { get; set; }

    public virtual DbSet<Ge36inspect> Ge36inspects { get; set; }

    public virtual DbSet<InfoLoco> InfoLocos { get; set; }

    public virtual DbSet<InfoLocosFinal> InfoLocosFinals { get; set; }

    public virtual DbSet<LeaseCoUser> LeaseCoUsers { get; set; }

    public virtual DbSet<LeftMidDoorInspect> LeftMidDoorInspects { get; set; }

    public virtual DbSet<MasterLoco> MasterLocos { get; set; }

    public virtual DbSet<MidPanInspect> MidPanInspects { get; set; }

    public virtual DbSet<RoofInspect> RoofInspects { get; set; }

    public virtual DbSet<ShortNoseInspect> ShortNoseInspects { get; set; }

    public virtual DbSet<TopRightPanInspect> TopRightPanInspects { get; set; }

    public virtual DbSet<WalkAroundInspect> WalkAroundInspects { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=XBSVM01\\SQLEXPRESS;Database=AVIDB;User Id=sa;Password=Codexx4b0s3;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.Property(e => e.InventoryNumber)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.LocoClass).HasMaxLength(50);
            entity.Property(e => e.LocoType)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.Lstat)
                .HasMaxLength(50)
                .HasColumnName("LStat");
            entity.Property(e => e.Move).HasMaxLength(50);
            entity.Property(e => e.NetBookValue)
                .HasMaxLength(100)
                .IsUnicode(false);
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
            entity.Property(e => e.Stype)
                .HasMaxLength(50)
                .HasColumnName("SType");
            entity.Property(e => e.TimeToRtsmonths).HasColumnName("TimeToRTSMonths");
            entity.Property(e => e.WorkScopeToRtsimmediately)
                .HasMaxLength(200)
                .HasColumnName("WorkScopeToRTSImmediately");
            entity.Property(e => e._619).HasMaxLength(50);
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
