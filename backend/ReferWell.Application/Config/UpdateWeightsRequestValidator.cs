using FluentValidation;

namespace ReferWell.Application.Config;

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
