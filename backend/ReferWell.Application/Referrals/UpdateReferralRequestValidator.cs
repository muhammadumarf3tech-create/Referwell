using FluentValidation;

namespace ReferWell.Application.Referrals;

public class UpdateReferralRequestValidator : AbstractValidator<UpdateReferralRequest>
{
    public UpdateReferralRequestValidator()
    {
        RuleFor(x => x.SpecialistType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}
