using FluentValidation;
using TaskManagement.Application.Requests;

namespace TaskManagement.Application.Validators;

public class CreateUserByAdminCommandValidator : AbstractValidator<CreateUserByAdminRequest>
{
    public CreateUserByAdminCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(50).WithMessage("Name must be at most 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(50).WithMessage("Email must be at most 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(50).WithMessage("Password must be at most 50 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role is required and must be valid.");
    }
}
