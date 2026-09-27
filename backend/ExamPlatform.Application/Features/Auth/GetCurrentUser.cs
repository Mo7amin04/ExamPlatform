using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Auth;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new CurrentUserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new UnauthorizedException("The account no longer exists or is inactive.");
    }
}
