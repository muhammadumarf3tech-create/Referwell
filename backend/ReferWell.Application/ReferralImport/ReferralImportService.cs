using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ReferWell.Application.Common.Interfaces;
using ReferWell.Application.Common.Models;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;
using ReferWell.Domain.Services;

namespace ReferWell.Application.ReferralImport;

public class ReferralImportService : IReferralImportService
{
    private static readonly string[] RequiredHeaders =
    [
        "NhiNumber", "PatientName", "DateOfBirth", "SpecialistType", "Reason", "Urgency"
    ];

    private static readonly HashSet<string> AllowedStatuses = Enum.GetNames<ReferralStatus>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedUrgencies = Enum.GetNames<UrgencyLevel>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReferralImportService(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AppResult> GetBatchesAsync(
        string? search = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;
        if (pageSize > 100) pageSize = 100;

        var query = _db.ReferralImportBatches
            .AsNoTracking()
            .Include(b => b.ImportedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b =>
                b.FileName.ToLower().Contains(term)
                || (b.ImportedByUser != null && b.ImportedByUser.FullName.ToLower().Contains(term))
                || b.Status.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        if (fromDate.HasValue)
            query = query.Where(b => b.StartedAt.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(b => b.StartedAt.Date <= toDate.Value.Date);

        var totalCount = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var items = await query
            .OrderByDescending(b => b.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id,
                b.FileName,
                b.Status,
                b.TotalRows,
                b.SucceededRows,
                b.FailedRows,
                b.CreatedPatients,
                b.Notes,
                b.StartedAt,
                b.CompletedAt,
                ImportedByUser = b.ImportedByUser == null
                    ? null
                    : new { b.ImportedByUser.Id, b.ImportedByUser.FullName, b.ImportedByUser.Email }
            })
            .ToListAsync(ct);

        return AppResult.Success(new { items, totalCount, page, pageSize, totalPages });
    }

    public async Task<AppResult> GetBatchAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _db.ReferralImportBatches
            .AsNoTracking()
            .Include(b => b.ImportedByUser)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (batch == null) return AppResult.NotFound("Import batch not found.");

        return AppResult.Success(new
        {
            batch.Id,
            batch.FileName,
            batch.Status,
            batch.TotalRows,
            batch.SucceededRows,
            batch.FailedRows,
            batch.CreatedPatients,
            batch.Notes,
            batch.StartedAt,
            batch.CompletedAt,
            ImportedByUser = batch.ImportedByUser == null
                ? null
                : new { batch.ImportedByUser.Id, batch.ImportedByUser.FullName, batch.ImportedByUser.Email }
        });
    }

    public async Task<AppResult> GetBatchRowsAsync(
        Guid id,
        string? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var exists = await _db.ReferralImportBatches.AnyAsync(b => b.Id == id, ct);
        if (!exists) return AppResult.NotFound("Import batch not found.");

        var query = _db.ReferralImportRows.AsNoTracking().Where(r => r.BatchId == id);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                (r.NhiNumber != null && r.NhiNumber.ToLower().Contains(term))
                || (r.PatientName != null && r.PatientName.ToLower().Contains(term))
                || (r.CaseNo != null && r.CaseNo.ToLower().Contains(term))
                || (r.LegacyCaseNo != null && r.LegacyCaseNo.ToLower().Contains(term))
                || (r.ErrorMessage != null && r.ErrorMessage.ToLower().Contains(term))
                || (r.SpecialistType != null && r.SpecialistType.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var items = await query
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.RowNumber,
                r.Status,
                r.NhiNumber,
                r.PatientName,
                r.SpecialistType,
                r.Urgency,
                r.ReferralStatus,
                r.LegacyCaseNo,
                r.CaseNo,
                r.ReferralId,
                r.PatientId,
                r.PatientCreated,
                r.ErrorColumn,
                r.ErrorMessage,
                r.RawData
            })
            .ToListAsync(ct);

        return AppResult.Success(new { items, totalCount, page, pageSize, totalPages });
    }

    public AppResult DownloadTemplate()
    {
        var csv = string.Join(",",
            "NhiNumber", "PatientName", "DateOfBirth", "PatientEmail", "PatientPhone", "Gender",
            "SpecialistType", "Reason", "Urgency", "Status", "ReceivedAt",
            "AssignedToEmail", "ReferringGpEmail", "LegacyCaseNo") + "\r\n";

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return AppResult.File(bytes, "text/csv", "referral-import-template.csv");
    }

    public async Task<AppResult> ImportAsync(string fileName, long fileLength, Stream content, CancellationToken ct = default)
    {
        if (fileLength == 0)
            return AppResult.BadRequest("Please upload a CSV file.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".csv" and not ".txt")
            return AppResult.BadRequest("Only .csv files are supported.");

        if (fileLength > 15_000_000)
            return AppResult.BadRequest("File exceeds the 15 MB limit.");

        string text;
        using (var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            text = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(text))
            return AppResult.BadRequest("The CSV file is empty.");

        List<string> headers;
        List<Dictionary<string, string>> dataRows;
        try
        {
            (headers, dataRows) = ParseCsv(text);
        }
        catch (Exception ex)
        {
            return AppResult.BadRequest($"Unable to parse CSV: {ex.Message}");
        }

        var missing = RequiredHeaders.Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();
        if (missing.Count > 0)
            return AppResult.BadRequest($"Missing required columns: {string.Join(", ", missing)}");

        if (dataRows.Count == 0)
            return AppResult.BadRequest("The CSV has a header but no data rows.");

        if (dataRows.Count > 12_000)
            return AppResult.BadRequest("Maximum 12,000 rows per import. Split the file and try again.");

        var batch = new ReferralImportBatch
        {
            FileName = Path.GetFileName(fileName),
            Status = "Processing",
            TotalRows = dataRows.Count,
            ImportedByUserId = _currentUser.UserId,
            StartedAt = DateTime.Now
        };
        _db.ReferralImportBatches.Add(batch);
        await _db.SaveChangesAsync(ct);

        try
        {
            var result = await ProcessImportAsync(batch, headers, dataRows, ct);
            batch.Status = "Completed";
            batch.SucceededRows = result.Succeeded;
            batch.FailedRows = result.Failed;
            batch.CreatedPatients = result.CreatedPatients;
            batch.CompletedAt = DateTime.Now;
            batch.Notes = $"{result.Succeeded} succeeded, {result.Failed} failed out of {dataRows.Count} rows.";
            await _db.SaveChangesAsync(ct);

            return AppResult.Success(new
            {
                batch.Id,
                batch.FileName,
                batch.Status,
                batch.TotalRows,
                batch.SucceededRows,
                batch.FailedRows,
                batch.CreatedPatients,
                batch.Notes,
                batch.StartedAt,
                batch.CompletedAt
            });
        }
        catch (Exception ex)
        {
            batch.Status = "Failed";
            batch.CompletedAt = DateTime.Now;
            batch.Notes = ex.Message;
            await _db.SaveChangesAsync(ct);
            return AppResult.ServerError(new { message = "Import failed.", detail = ex.Message, batchId = batch.Id });
        }
    }

    private async Task<(int Succeeded, int Failed, int CreatedPatients)> ProcessImportAsync(
        ReferralImportBatch batch,
        List<string> headers,
        List<Dictionary<string, string>> dataRows,
        CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId;
        var weights = await GetWeights(ct);
        var users = await _db.Users.AsNoTracking().ToListAsync(ct);
        var usersByEmail = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .GroupBy(u => u.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var nhiNumbers = dataRows
            .Select(r => Get(r, "NhiNumber"))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingPatients = await _db.Patients
            .Where(p => nhiNumbers.Contains(p.NhiNumber))
            .ToListAsync(ct);
        var patientsByNhi = existingPatients
            .GroupBy(p => p.NhiNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var nextCaseIndex = await GetNextCaseIndexAsync(ct);
        var existingCaseNos = await _db.Referrals.AsNoTracking().Select(r => r.CaseNo).ToListAsync(ct);
        var usedCaseNos = new HashSet<string>(existingCaseNos, StringComparer.OrdinalIgnoreCase);

        var rowResults = new List<ReferralImportRow>(dataRows.Count);
        var referralsToAdd = new List<Referral>();
        var auditsToAdd = new List<AuditLog>();
        var patientsToAdd = new List<Patient>();
        int succeeded = 0, failed = 0, createdPatients = 0;

        for (var i = 0; i < dataRows.Count; i++)
        {
            var rowNumber = i + 2; // header is row 1
            var cells = dataRows[i];
            var report = new ReferralImportRow
            {
                BatchId = batch.Id,
                RowNumber = rowNumber,
                NhiNumber = Truncate(Get(cells, "NhiNumber"), 50),
                PatientName = Truncate(Get(cells, "PatientName"), 200),
                SpecialistType = Truncate(Get(cells, "SpecialistType"), 100),
                Urgency = Get(cells, "Urgency"),
                ReferralStatus = Get(cells, "Status"),
                LegacyCaseNo = Truncate(Get(cells, "LegacyCaseNo"), 50),
                RawData = Truncate(SerializeRow(headers, cells), 4000)
            };

            var error = ValidateAndBuild(
                cells,
                report,
                usersByEmail,
                patientsByNhi,
                usedCaseNos,
                currentUserId,
                weights,
                ref nextCaseIndex,
                out var referral,
                out var newPatient);

            if (error != null)
            {
                report.Status = "Failed";
                report.ErrorColumn = error.Column;
                report.ErrorMessage = error.Message;
                failed++;
                rowResults.Add(report);
                continue;
            }

            if (newPatient != null)
            {
                patientsToAdd.Add(newPatient);
                patientsByNhi[newPatient.NhiNumber] = newPatient;
                createdPatients++;
                report.PatientCreated = true;
                report.PatientId = newPatient.Id;
            }
            else
            {
                report.PatientId = referral!.PatientId;
            }

            referralsToAdd.Add(referral!);
            auditsToAdd.Add(new AuditLog
            {
                ReferralId = referral!.Id,
                PerformedByUserId = currentUserId,
                Action = "Imported",
                ToStatus = referral.Status,
                Notes = string.IsNullOrWhiteSpace(report.LegacyCaseNo)
                    ? $"CSV import batch {batch.Id}"
                    : $"CSV import batch {batch.Id}; legacy case {report.LegacyCaseNo}"
            });

            report.Status = "Succeeded";
            report.CaseNo = referral.CaseNo;
            report.ReferralId = referral.Id;
            report.ReferralStatus = referral.Status.ToString();
            report.Urgency = referral.Urgency.ToString();
            succeeded++;
            rowResults.Add(report);
        }

        if (patientsToAdd.Count > 0)
            _db.Patients.AddRange(patientsToAdd);
        if (referralsToAdd.Count > 0)
            _db.Referrals.AddRange(referralsToAdd);
        if (auditsToAdd.Count > 0)
            _db.AuditLogs.AddRange(auditsToAdd);

        _db.ReferralImportRows.AddRange(rowResults);
        await _db.SaveChangesAsync(ct);

        return (succeeded, failed, createdPatients);
    }

    private sealed record RowError(string Column, string Message);

    private static RowError? ValidateAndBuild(
        Dictionary<string, string> cells,
        ReferralImportRow report,
        Dictionary<string, ApplicationUser> usersByEmail,
        Dictionary<string, Patient> patientsByNhi,
        HashSet<string> usedCaseNos,
        Guid currentUserId,
        (double urgency, double waittime, double patient) weights,
        ref int nextCaseIndex,
        out Referral? referral,
        out Patient? newPatient)
    {
        referral = null;
        newPatient = null;

        var nhi = Get(cells, "NhiNumber").Trim();
        var patientName = Get(cells, "PatientName").Trim();
        var dobRaw = Get(cells, "DateOfBirth").Trim();
        var specialistType = Get(cells, "SpecialistType").Trim();
        var reason = Get(cells, "Reason").Trim();
        var urgencyRaw = Get(cells, "Urgency").Trim();
        var statusRaw = Get(cells, "Status").Trim();
        var receivedAtRaw = Get(cells, "ReceivedAt").Trim();
        var assignedEmail = Get(cells, "AssignedToEmail").Trim();
        var referringEmail = Get(cells, "ReferringGpEmail").Trim();
        var legacyCaseNo = Get(cells, "LegacyCaseNo").Trim();
        var patientEmail = Get(cells, "PatientEmail").Trim();
        var patientPhone = Get(cells, "PatientPhone").Trim();
        var gender = Get(cells, "Gender").Trim();

        if (string.IsNullOrWhiteSpace(nhi))
            return new RowError("NhiNumber", "NHI number is required.");
        if (nhi.Length > 50)
            return new RowError("NhiNumber", "NHI number must be 50 characters or fewer.");

        if (string.IsNullOrWhiteSpace(specialistType))
            return new RowError("SpecialistType", "Specialist type is required.");
        if (specialistType.Length > 100)
            return new RowError("SpecialistType", "Specialist type must be 100 characters or fewer.");

        if (string.IsNullOrWhiteSpace(reason))
            return new RowError("Reason", "Reason is required.");
        if (reason.Length > 2000)
            return new RowError("Reason", "Reason must be 2000 characters or fewer.");

        if (string.IsNullOrWhiteSpace(urgencyRaw))
            return new RowError("Urgency", "Urgency is required (Routine, SemiUrgent, Urgent).");
        if (!Enum.TryParse<UrgencyLevel>(urgencyRaw, ignoreCase: true, out var urgency)
            || !AllowedUrgencies.Contains(urgencyRaw))
            return new RowError("Urgency", $"Invalid urgency '{urgencyRaw}'. Use Routine, SemiUrgent, or Urgent.");

        var status = ReferralStatus.Received;
        if (!string.IsNullOrWhiteSpace(statusRaw))
        {
            if (!Enum.TryParse<ReferralStatus>(statusRaw, ignoreCase: true, out status)
                || !AllowedStatuses.Contains(statusRaw))
                return new RowError("Status", $"Invalid status '{statusRaw}'. Use Received, Triaged, Accepted, Declined, Booked, or Completed.");
        }

        DateTime receivedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(receivedAtRaw))
        {
            if (!TryParseDateTime(receivedAtRaw, out receivedAt))
                return new RowError("ReceivedAt", $"Invalid date/time '{receivedAtRaw}'. Use yyyy-MM-dd or ISO format.");
        }

        DateTime? dob = null;
        if (!string.IsNullOrWhiteSpace(dobRaw))
        {
            if (!TryParseDate(dobRaw, out var parsedDob))
                return new RowError("DateOfBirth", $"Invalid date of birth '{dobRaw}'. Use yyyy-MM-dd.");
            dob = parsedDob;
            if (dob > DateTime.Today)
                return new RowError("DateOfBirth", "Date of birth cannot be in the future.");
        }

        Guid assignedToUserId = currentUserId;
        if (!string.IsNullOrWhiteSpace(assignedEmail))
        {
            if (!usersByEmail.TryGetValue(assignedEmail, out var assignee))
                return new RowError("AssignedToEmail", $"User not found for email '{assignedEmail}'.");
            assignedToUserId = assignee.Id;
        }

        Guid createdByUserId = currentUserId;
        string referringGpId = currentUserId.ToString();
        if (!string.IsNullOrWhiteSpace(referringEmail))
        {
            if (!usersByEmail.TryGetValue(referringEmail, out var gp))
                return new RowError("ReferringGpEmail", $"User not found for email '{referringEmail}'.");
            createdByUserId = gp.Id;
            referringGpId = gp.Id.ToString();
        }

        // Preserve legacy case number as CaseNo when provided
        string caseNo;
        if (!string.IsNullOrWhiteSpace(legacyCaseNo))
        {
            if (legacyCaseNo.Length > 50)
                return new RowError("LegacyCaseNo", "Legacy case number must be 50 characters or fewer.");
            if (!usedCaseNos.Add(legacyCaseNo))
                return new RowError("LegacyCaseNo", $"Case number '{legacyCaseNo}' already exists (duplicate in file or database).");
            caseNo = legacyCaseNo;
        }
        else
        {
            do
            {
                caseNo = $"Ref-{nextCaseIndex:D6}";
                nextCaseIndex++;
            } while (!usedCaseNos.Add(caseNo));
        }

        Patient patient;
        if (patientsByNhi.TryGetValue(nhi, out var existing))
        {
            if (dob.HasValue && existing.DateOfBirth.Date != dob.Value.Date)
                return new RowError("DateOfBirth", $"Patient NHI '{nhi}' exists but DateOfBirth does not match ({existing.DateOfBirth:yyyy-MM-dd}).");
            if (!string.IsNullOrWhiteSpace(patientName)
                && !string.Equals(existing.Name.Trim(), patientName, StringComparison.OrdinalIgnoreCase))
            {
                // Soft conflict: allow import but keep existing patient name
            }
            patient = existing;
            if (!dob.HasValue)
                dob = existing.DateOfBirth;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(patientName))
                return new RowError("PatientName", "Patient name is required when creating a new patient.");
            if (!dob.HasValue)
                return new RowError("DateOfBirth", "Date of birth is required when creating a new patient.");
            if (patientName.Length > 200)
                return new RowError("PatientName", "Patient name must be 200 characters or fewer.");
            if (gender.Length > 20)
                return new RowError("Gender", "Gender must be 20 characters or fewer.");

            newPatient = new Patient
            {
                Name = patientName,
                DateOfBirth = dob.Value.Date,
                Email = patientEmail,
                PhoneNumber = patientPhone,
                NhiNumber = nhi,
                Gender = gender,
                CreatedAt = DateTime.Now
            };
            patient = newPatient;
        }

        var slaDeadline = Referral.CalculateSlaDeadline(urgency, receivedAt);
        var entity = new Referral
        {
            PatientId = patient.Id,
            CaseNo = caseNo,
            IsMigrated = true,
            SpecialistType = specialistType,
            Reason = reason,
            Urgency = urgency,
            Status = status,
            ReceivedAt = receivedAt,
            SlaDeadline = slaDeadline,
            CreatedByUserId = createdByUserId,
            ReferringGPId = referringGpId,
            AssignedToUserId = assignedToUserId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        entity.EvaluateSlaBreach(DateTime.Now);
        entity.PriorityScore = PriorityCalculator.Calculate(
            entity.Urgency, entity.ReceivedAt, dob ?? patient.DateOfBirth,
            weights.urgency, weights.waittime, weights.patient);

        referral = entity;
        return null;
    }

    private async Task<int> GetNextCaseIndexAsync(CancellationToken ct)
    {
        var existing = await _db.Referrals.AsNoTracking()
            .Select(r => r.CaseNo)
            .ToListAsync(ct);
        return CaseNoGenerator.MaxSequence(existing) + 1;
    }

    private async Task<(double urgency, double waittime, double patient)> GetWeights(CancellationToken ct)
    {
        var configs = await _db.SystemConfigs.ToListAsync(ct);
        double GetWeight(string key, double def) =>
            double.TryParse(configs.FirstOrDefault(c => c.Key == key)?.Value, out var v) ? v : def;
        return (GetWeight("weight_urgency", 50), GetWeight("weight_waittime", 30), GetWeight("weight_patient", 20));
    }

    private static string Get(Dictionary<string, string> cells, string key) =>
        cells.TryGetValue(key, out var value) ? value?.Trim() ?? "" : "";

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max];
    }

    private static string SerializeRow(List<string> headers, Dictionary<string, string> cells)
    {
        var parts = headers.Select(h =>
        {
            cells.TryGetValue(h, out var v);
            v ??= "";
            if (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
                return $"\"{v.Replace("\"", "\"\"")}\"";
            return v;
        });
        return string.Join(",", parts);
    }

    private static bool TryParseDate(string raw, out DateTime date)
    {
        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return true;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }

    private static bool TryParseDateTime(string raw, out DateTime date)
    {
        var formats = new[]
        {
            "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "dd/MM/yyyy HH:mm"
        };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return true;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }

    private static (List<string> Headers, List<Dictionary<string, string>> Rows) ParseCsv(string content)
    {
        var lines = SplitCsvLines(content);
        if (lines.Count == 0)
            throw new InvalidOperationException("File has no lines.");

        var headers = ParseCsvLine(lines[0])
            .Select(h => h.Trim().TrimStart('\uFEFF'))
            .ToList();

        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Header row is empty.");

        // Normalize known header aliases
        var normalized = headers.Select(NormalizeHeader).ToList();

        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var values = ParseCsvLine(lines[i]);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < normalized.Count; c++)
            {
                var key = normalized[c];
                if (string.IsNullOrWhiteSpace(key)) continue;
                dict[key] = c < values.Count ? values[c] : "";
            }
            // Skip completely empty data rows
            if (dict.Values.All(string.IsNullOrWhiteSpace)) continue;
            rows.Add(dict);
        }

        return (normalized, rows);
    }

    private static string NormalizeHeader(string header)
    {
        var h = header.Trim();
        return h.ToLowerInvariant() switch
        {
            "nhi" or "nhi_number" or "nhi number" => "NhiNumber",
            "patient" or "patient_name" or "patient name" or "name" => "PatientName",
            "dob" or "date_of_birth" or "date of birth" => "DateOfBirth",
            "email" or "patient_email" or "patient email" => "PatientEmail",
            "phone" or "patient_phone" or "patient phone" or "phonenumber" => "PatientPhone",
            "specialty" or "specialist" or "specialist_type" or "specialist type" => "SpecialistType",
            "referral_reason" or "referral reason" => "Reason",
            "urgency_level" or "urgency level" => "Urgency",
            "referral_status" or "referral status" => "Status",
            "received" or "received_at" or "received at" or "receiveddate" => "ReceivedAt",
            "assignedto" or "assigned_to" or "assigned to" or "assignee" or "assigned_to_email" => "AssignedToEmail",
            "referringgp" or "referring_gp" or "referring gp" or "gp_email" or "referring_gp_email" => "ReferringGpEmail",
            "legacy_case_no" or "legacy case no" or "external_case_no" or "externalcaseno" or "old_case_no" => "LegacyCaseNo",
            _ => h
        };
    }

    private static List<string> SplitCsvLines(string content)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(ch);
                continue;
            }
            if (!inQuotes && (ch == '\n' || ch == '\r'))
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(ch);
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
