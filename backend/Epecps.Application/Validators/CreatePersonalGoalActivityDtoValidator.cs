using Epecps.Application.DTOs.EmployeeGoals;
using FluentValidation;

namespace Epecps.Application.Validators;

/// <summary>
/// Validator for CreatePersonalGoalActivityDto
/// </summary>
public class CreatePersonalGoalActivityDtoValidator : AbstractValidator<CreatePersonalGoalActivityDto>
{
    public CreatePersonalGoalActivityDtoValidator()
    {
        // Description is REQUIRED - no suggested activities allowed
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.");

        // Suggested activity ID is ignored (will always be null)
    }
}
