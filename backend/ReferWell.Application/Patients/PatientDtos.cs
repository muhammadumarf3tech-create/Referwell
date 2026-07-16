namespace ReferWell.Application.Patients;

public record CreatePatientRequest(
    string Name,
    DateTime DateOfBirth,
    string Email,
    string? PhoneNumber,
    string NhiNumber,
    string? Gender);

public record UpdatePatientRequest(
    string Name,
    DateTime DateOfBirth,
    string Email,
    string? PhoneNumber,
    string NhiNumber,
    string? Gender);
