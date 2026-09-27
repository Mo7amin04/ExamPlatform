using ExamPlatform.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Departments;

public sealed record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

public sealed class GetDepartmentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken) =>
        await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.Description, d.Courses.Count, d.CreatedAt))
            .ToListAsync(cancellationToken);
}
