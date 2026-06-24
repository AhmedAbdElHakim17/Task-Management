using FluentValidation;
using TaskManagement.Application.Requests;

namespace TaskManagement.Application.Validators;

public class LoginUserCommandValidator : AbstractValidator<LoginRequest>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(50).WithMessage("Email must be at most 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(50).WithMessage("Password must be at most 50 characters.");
    }
}
