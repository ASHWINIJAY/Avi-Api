using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class LocoDashboard
{
    public int Id { get; set; }

    public string? InspectorId { get; set; }

    public string? InspectorName { get; set; }

    public int? LocoNumber { get; set; }

    public string? LocoClass { get; set; }

    public string? LocoModel { get; set; }

    public string? DateAssessed { get; set; }

    public string? TimeAssessed { get; set; }

    public string? LocoPhoto { get; set; }

    public string? BodyDamage { get; set; }

    public string? BodyPhotos { get; set; }

    public string? RefurbishValue { get; set; }

    public string? MissingValue { get; set; }

    public string? ReplaceValue { get; set; }

    public string? MissingPhotos { get; set; }

    public string? ReplacePhotos { get; set; }

    public string? AssessmentQuote { get; set; }

    public string? AssessmentCert { get; set; }

    public string? UploadStatus { get; set; }

    public string? UploadDate { get; set; }

    public string? GpsLatitude { get; set; }

    public string? GpsLongitude { get; set; }

    public string? StartTimeInspect { get; set; }

    public string? ReplacementValue { get; set; }

    public string? TotalLaborValue { get; set; }

    public string? AssetValue { get; set; }

    public string? AssessmentSow { get; set; }

    public string? TotalValue { get; set; }

    public string? MarketValue { get; set; }

    public int? ConditionScore { get; set; }

    public string? OperationalStatus { get; set; }

    public string? City { get; set; }

    public int? CalScore { get; set; }

    public string? CalOperateStatus { get; set; }

    public string? CalCondition { get; set; }

    public int Phase { get; set; }
}
