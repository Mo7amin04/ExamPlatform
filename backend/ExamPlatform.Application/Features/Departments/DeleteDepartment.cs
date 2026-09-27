using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Departments;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest;

public sealed class DeleteDepartmentCommandHandler(IApplicationDbContext db) : IRequestHandler<DeleteDepartmentCommand>
{
    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        if (await db.Courses.AnyAsync(c => c.DepartmentId == request.Id, cancellationToken))
            throw new ConflictException("The department still has courses. Move or delete them first.");

        db.Departments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
    }
}
