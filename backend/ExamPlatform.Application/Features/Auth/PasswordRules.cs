using FluentValidation;

namespace ExamPlatform.Application.Features.Auth;

internal static class PasswordRules
{
    public const int MaximumLength = 128;

    public static void Apply<T>(IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Enter a password.")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
        .MaximumLength(MaximumLength).WithMessage("Password must be at most 128 characters.")
        .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
        .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.");
}
