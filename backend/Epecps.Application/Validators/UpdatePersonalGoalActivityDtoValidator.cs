using Epecps.Application.DTOs.EmployeeGoals;
using FluentValidation;

namespace Epecps.Application.Validators;

/// <summary>
/// Validator for UpdatePersonalGoalActivityDto
/// </summary>
public class UpdatePersonalGoalActivityDtoValidator : AbstractValidator<UpdatePersonalGoalActivityDto>
{
    public UpdatePersonalGoalActivityDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid activity status.");

        RuleFor(x => x.EvidenceUrl)
            .MaximumLength(2000)
            .WithMessage("Evidence URL cannot exceed 2000 characters.");

        RuleFor(x => x.EvidenceNotes)
            .MaximumLength(2000)
            .WithMessage("Evidence notes cannot exceed 2000 characters.");
    }
}
