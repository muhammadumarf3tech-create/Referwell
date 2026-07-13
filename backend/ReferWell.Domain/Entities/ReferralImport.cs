namespace ReferWell.Domain.Entities;

/// <summary>One uploaded CSV import run with aggregate counts and status.</summary>
public class ReferralImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed"; // Processing, Completed, Failed
    public int TotalRows { get; set; }
    public int SucceededRows { get; set; }
    public int FailedRows { get; set; }
    public int CreatedPatients { get; set; }
    public string? Notes { get; set; }
    public Guid ImportedByUserId { get; set; }
    public ApplicationUser? ImportedByUser { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public ICollection<ReferralImportRow> Rows { get; set; } = new List<ReferralImportRow>();
}

/// <summary>Per-row validation / import outcome for reporting.</summary>
public class ReferralImportRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public ReferralImportBatch? Batch { get; set; }
    public int RowNumber { get; set; }
    public string Status { get; set; } = "Failed"; // Succeeded, Failed, Skipped
    public string? NhiNumber { get; set; }
    public string? PatientName { get; set; }
    public string? SpecialistType { get; set; }
    public string? Urgency { get; set; }
    public string? ReferralStatus { get; set; }
    public string? LegacyCaseNo { get; set; }
    public string? CaseNo { get; set; }
    public Guid? ReferralId { get; set; }
    public Guid? PatientId { get; set; }
    public bool PatientCreated { get; set; }
    public string? ErrorColumn { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawData { get; set; }
}
