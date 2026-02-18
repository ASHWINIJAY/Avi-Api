using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class WagonDashboardUploaded
{
    public int Id { get; set; }

    public string InspectorId { get; set; } = null!;

    public string InspectorName { get; set; } = null!;

    public int WagonNumber { get; set; }

    public string WagonGroup { get; set; } = null!;

    public string WagonType { get; set; } = null!;

    public string DateAssessed { get; set; } = null!;

    public string TimeAssessed { get; set; } = null!;

    public string BodyDamage { get; set; } = null!;

    public string? BodyPhotos { get; set; }

    public string? LiftPhoto { get; set; }

    public string? LiftDate { get; set; }

    public string? LiftLapsed { get; set; }

    public string? BarrelPhoto { get; set; }

    public string? BarrelDate { get; set; }

    public string? BarrelLapsed { get; set; }

    public string? BrakePhoto { get; set; }

    public string? BrakeDate { get; set; }

    public string? BrakeLapsed { get; set; }

    public string RefurbishValue { get; set; } = null!;

    public string MissingValue { get; set; } = null!;

    public string ReplaceValue { get; set; } = null!;

    public string? AssessmentQuote { get; set; }

    public string? AssessmentCert { get; set; }

    public string WagonStatus { get; set; } = null!;

    public string UploadDate { get; set; } = null!;

    public string? WagonPhoto { get; set; }

    public string? MissingPhotos { get; set; }

    public string? ReplacePhotos { get; set; }

    public string? GpsLatitude { get; set; }

    public string? GpsLongitude { get; set; }

    public string? StartTimeInspect { get; set; }

    public string? MarketValue { get; set; }

    public string? TotalLaborValue { get; set; }

    public string? AssetValue { get; set; }

    public string? AssessmentSow { get; set; }

    public string? LiftValue { get; set; }

    public string? BarrelValue { get; set; }

    public string? TotalValue { get; set; }

    public int? ConditionScore { get; set; }

    public string? OperationalStatus { get; set; }

    public string? City { get; set; }

    public int? CalScore { get; set; }

    public string? CalOperateStatus { get; set; }

    public string? CalCondition { get; set; }

    public int Phase { get; set; }
}
