using FluentValidation;
using TaskManagement.Application.Requests;

namespace TaskManagement.Application.Validators;

public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Task status is required and must be valid.");
    }
}
