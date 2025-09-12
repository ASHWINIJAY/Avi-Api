using System;
using System.Collections.Generic;

namespace AviFinal.Api.Models;

public partial class DashBoardItem
{
    public string Record { get; set; } = null!;

    public int LocoNumber { get; set; }

    public DateOnly DateAssessed { get; set; }

    public TimeOnly TimeAssessed { get; set; }

    public string InspectorName { get; set; } = null!;

    public string ProMain { get; set; } = null!;

    public string BodyDamage { get; set; } = null!;

    public string? BodyPhotos { get; set; }

    public string? BodyRepairValue { get; set; }

    public string? ReplaceValue { get; set; }

    public string? RefurbishValue { get; set; }

    public string LiftingRequired { get; set; } = null!;

    public string? LiftPhotos { get; set; }

    public DateOnly? LiftDate { get; set; }

    public string? AssessmentResults { get; set; }

    public string? AssessmentPhotos { get; set; }

    public string? AssessmentQuote { get; set; }

    public string? AssessmentCert { get; set; }

    public string UploadStatus { get; set; } = null!;

    public DateOnly UploadDate { get; set; }

    public virtual MasterLoco LocoNumberNavigation { get; set; } = null!;
}
