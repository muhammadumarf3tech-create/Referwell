using FluentValidation;

namespace ReferWell.Application.Users;

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
