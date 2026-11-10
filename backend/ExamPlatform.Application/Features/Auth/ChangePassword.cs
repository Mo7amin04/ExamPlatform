using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Enter your current password.")
            .MaximumLength(PasswordRules.MaximumLength).WithMessage("Password must be at most 128 characters.");
        PasswordRules.Apply(RuleFor(x => x.NewPassword));
        RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must differ from the current password.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedException();

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new ValidationException(nameof(request.CurrentPassword), "The current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
    }
}
