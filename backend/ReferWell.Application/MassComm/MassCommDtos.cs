namespace ReferWell.Application.MassComm;

public record CreateCampaignRequest(
    string Name,
    string SubjectTemplate,
    string BodyTemplate,
    string RecipientType,
    CampaignFilters? Filters);

public record CampaignFilters(
    List<string>? Urgencies = null,
    List<string>? Statuses = null,
    List<string>? SpecialistTypes = null,
    List<string>? AssignedToUserIds = null,
    bool? OnlySlaBreached = null,
    DateTime? ReceivedFrom = null,
    DateTime? ReceivedTo = null,
    string? CaseNo = null);
