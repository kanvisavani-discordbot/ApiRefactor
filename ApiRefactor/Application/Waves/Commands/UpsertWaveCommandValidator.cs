using FluentValidation;

namespace ApiRefactor.Application.Waves.Commands;

public sealed class UpsertWaveCommandValidator : AbstractValidator<UpsertWaveCommand>
{
    public UpsertWaveCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.WaveDate)
            .Must(d => d is null || d.Value.Kind == DateTimeKind.Utc)
            .WithMessage("WaveDate must be UTC when supplied.");
    }
}
