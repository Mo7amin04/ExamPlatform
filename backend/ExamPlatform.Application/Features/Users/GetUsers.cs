using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Users;

public sealed record GetUsersQuery(string? Role = null, string? Search = null) : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetUsersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == request.Role));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id, u.FullName, u.Email, u.IsActive,
                u.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToList(),
                u.CreatedAt, u.LastLoginAt))
            .ToListAsync(cancellationToken);
    }
}
