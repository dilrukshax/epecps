using Epecps.Application.DTOs.EmployeeGoals;
using FluentValidation;

namespace Epecps.Application.Validators;

/// <summary>
/// Validator for UpdatePersonalGoalDto
/// </summary>
public class UpdatePersonalGoalDtoValidator : AbstractValidator<UpdatePersonalGoalDto>
{
    public UpdatePersonalGoalDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required.")
            .LessThanOrEqualTo(x => x.DueDate)
            .WithMessage("Start date must be before or equal to due date.");

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .WithMessage("Due date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Due date must be after or equal to start date.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid goal status.");
    }
}
