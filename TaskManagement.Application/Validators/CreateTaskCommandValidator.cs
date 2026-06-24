using FluentValidation;
using TaskManagement.Application.Requests;

namespace TaskManagement.Application.Validators;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(5).WithMessage("Description must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Description must be at most 500 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Priority is required and must be a valid value.");
    }
}
