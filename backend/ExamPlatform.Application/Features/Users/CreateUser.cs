using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Users;

public sealed record CreateUserCommand(string FullName, string Email, string Password, IReadOnlyList<string> Roles)
    : IRequest<UserDto>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.Roles).NotEmpty().WithMessage("At least one role is required.");
        RuleForEach(x => x.Roles)
            .Must(r => RoleNames.All.Contains(r))
            .WithMessage("Unknown role '{PropertyValue}'.");
    }
}

public sealed class CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher passwordHasher)
    : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        var roles = await db.Roles.Where(r => request.Roles.Contains(r.Name)).ToListAsync(cancellationToken);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true
        };
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role, RoleId = role.Id });

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.FullName, user.Email, user.IsActive,
            roles.Select(r => r.Name).OrderBy(r => r).ToList(), user.CreatedAt, null);
    }
}
