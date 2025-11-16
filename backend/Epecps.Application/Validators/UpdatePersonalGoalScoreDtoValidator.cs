using Epecps.Application.DTOs.EmployeeGoals;
using FluentValidation;

namespace Epecps.Application.Validators;

/// <summary>
/// Validator for UpdatePersonalGoalScoreDto
/// </summary>
public class UpdatePersonalGoalScoreDtoValidator : AbstractValidator<UpdatePersonalGoalScoreDto>
{
    public UpdatePersonalGoalScoreDtoValidator()
    {
        RuleFor(x => x.CurrentScore)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Current score must be greater than or equal to 0.");
    }
}
