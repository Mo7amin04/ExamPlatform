using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Users;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest;

public sealed class SetUserActiveCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetUserActiveCommand>
{
    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == currentUser.UserId && !request.IsActive)
            throw new ConflictException("You cannot deactivate your own account.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
    }
}
