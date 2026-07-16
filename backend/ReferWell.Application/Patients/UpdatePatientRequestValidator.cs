using FluentValidation;

namespace ReferWell.Application.Patients;

public class UpdatePatientRequestValidator : AbstractValidator<UpdatePatientRequest>
{
    public UpdatePatientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NhiNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today.AddDays(1));
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).MaximumLength(40).When(x => x.PhoneNumber != null);
        RuleFor(x => x.Gender).MaximumLength(30).When(x => x.Gender != null);
    }
}
