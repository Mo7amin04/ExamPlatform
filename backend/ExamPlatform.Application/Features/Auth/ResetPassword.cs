using System.Security.Cryptography;
using System.Text;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Auth;

public sealed record PasswordResetTicket(Guid UserId, string PasswordVersion)
{
    public static string VersionOf(string passwordHash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));
}

public interface IPasswordResetTokens
{
    PasswordResetTicket? Read(string token);
}

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
        PasswordRules.Apply(RuleFor(x => x.NewPassword));
    }
}

public sealed class ResetPasswordCommandHandler(IApplicationDbContext db, IPasswordResetTokens tokens, IPasswordHasher hasher)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        const string invalidLink = "This password reset link is invalid or has expired.";
        var ticket = tokens.Read(request.Token) ?? throw new ValidationException(nameof(request.Token), invalidLink);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == ticket.UserId && u.IsActive, cancellationToken);
        if (user is null || ticket.PasswordVersion != PasswordResetTicket.VersionOf(user.PasswordHash))
            throw new ValidationException(nameof(request.Token), invalidLink);

        // PasswordHash is a concurrency token, so two requests cannot redeem the same link.
        user.PasswordHash = hasher.Hash(request.NewPassword);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException(nameof(request.Token), invalidLink);
        }
    }
}
