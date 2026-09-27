using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Auth;

public sealed record CurrentUserDto(Guid Id, string FullName, string Email, IReadOnlyList<string> Roles);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, CurrentUserDto User);

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, LoginResponse>
{
    private const string InvalidCredentials = "Invalid email or password.";

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // Spend comparable time hashing so response timing does not reveal whether the account exists.
            passwordHasher.Hash(request.Password);
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password) || !user.IsActive)
            throw new UnauthorizedException(InvalidCredentials);

        user.LastLoginAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToList();
        var token = tokenGenerator.Generate(user, roles);

        return new LoginResponse(token.Token, token.ExpiresAt,
            new CurrentUserDto(user.Id, user.FullName, user.Email, roles));
    }
}
