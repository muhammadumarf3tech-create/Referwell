using FluentValidation;
using ReferWell.Api.Controllers;

namespace ReferWell.Api.Validation;

public class LoginRequestValidator : AbstractValidator<AuthController.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.Roles).NotNull().Must(r => r.Count > 0).WithMessage("At least one role is required.");
        RuleFor(x => x.Title).MaximumLength(50).When(x => x.Title != null);
        RuleFor(x => x.Gender).MaximumLength(30).When(x => x.Gender != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(40).When(x => x.PhoneNumber != null);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Roles).NotNull().Must(r => r.Count > 0).WithMessage("At least one role is required.");
        RuleFor(x => x.NewPassword)
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.")
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        RuleFor(x => x.Title).MaximumLength(50).When(x => x.Title != null);
        RuleFor(x => x.Gender).MaximumLength(30).When(x => x.Gender != null);
        RuleFor(x => x.PhoneNumber).MaximumLength(40).When(x => x.PhoneNumber != null);
    }
}

public class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NhiNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).LessThan(DateTime.Today.AddDays(1));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PhoneNumber).MaximumLength(40).When(x => x.PhoneNumber != null);
        RuleFor(x => x.Gender).MaximumLength(30).When(x => x.Gender != null);
    }
}

public class CreateReferralRequestValidator : AbstractValidator<CreateReferralRequest>
{
    public CreateReferralRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.SpecialistType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}

public class UpdateReferralRequestValidator : AbstractValidator<UpdateReferralRequest>
{
    public UpdateReferralRequestValidator()
    {
        RuleFor(x => x.SpecialistType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}

public class UpdateWeightsRequestValidator : AbstractValidator<UpdateWeightsRequest>
{
    public UpdateWeightsRequestValidator()
    {
        RuleFor(x => x.WeightUrgency).InclusiveBetween(0, 100);
        RuleFor(x => x.WeightWaittime).InclusiveBetween(0, 100);
        RuleFor(x => x.WeightPatient).InclusiveBetween(0, 100);
        RuleFor(x => x)
            .Must(x => Math.Abs(x.WeightUrgency + x.WeightWaittime + x.WeightPatient - 100) <= 0.01)
            .WithMessage("Weights must sum to 100%.");
    }
}

public class TransitionRequestValidator : AbstractValidator<TransitionRequest>
{
    public TransitionRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
        RuleFor(x => x.RowVersion).NotNull();
    }
}
