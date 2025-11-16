using Epecps.Application.DTOs.EmployeeGoals;
using FluentValidation;

namespace Epecps.Application.Validators;

/// <summary>
/// Validator for CreatePersonalGoalDto
/// </summary>
public class CreatePersonalGoalDtoValidator : AbstractValidator<CreatePersonalGoalDto>
{
    public CreatePersonalGoalDtoValidator()
    {
        RuleFor(x => x.GoalItemId)
            .NotEmpty()
            .WithMessage("Goal item ID is required.");

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

        // SelectedSuggestedActivityIds is now optional and ignored
        RuleFor(x => x.SelectedSuggestedActivityIds)
            .NotNull()
            .WithMessage("Selected suggested activity IDs list cannot be null.");

        RuleFor(x => x.CustomActivities)
            .NotNull()
            .WithMessage("Custom activities list cannot be null.");

        // Changed: Only require custom activities (manual activities)
        RuleFor(x => x.CustomActivities)
            .Must(activities => activities != null && activities.Any())
            .WithMessage("At least one activity must be provided.");
        
        // Validate each custom activity is not empty
        RuleForEach(x => x.CustomActivities)
            .NotEmpty()
            .WithMessage("Activity description cannot be empty.")
            .MaximumLength(1000)
            .WithMessage("Activity description cannot exceed 1000 characters.");
    }
}
