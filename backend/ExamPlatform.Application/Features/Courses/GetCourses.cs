using ExamPlatform.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

/// <summary>Lists courses visible to the caller (all for admins, assigned ones for teachers).</summary>
public sealed record GetCoursesQuery(Guid? DepartmentId = null, string? Search = null) : IRequest<IReadOnlyList<CourseDto>>;

public sealed class GetCoursesQueryHandler(ICourseAccessService access)
    : IRequestHandler<GetCoursesQuery, IReadOnlyList<CourseDto>>
{
    public async Task<IReadOnlyList<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var query = access.AccessibleCourses().AsNoTracking();

        if (request.DepartmentId is { } departmentId)
            query = query.Where(c => c.DepartmentId == departmentId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Code.Contains(term));
        }

        return await query
            .OrderBy(c => c.Code)
            .Select(c => new CourseDto(
                c.Id,
                c.DepartmentId,
                c.Department.Name,
                c.Code,
                c.Name,
                c.Description,
                c.CreditHours,
                c.Topics.Count,
                c.Questions.Count(q => q.Status != Domain.Enums.QuestionStatus.Archived),
                c.Exams.Count(e => e.Status != Domain.Enums.ExamStatus.Archived),
                c.CourseTeachers
                    .OrderBy(t => t.Teacher.FullName)
                    .Select(t => new CourseTeacherDto(t.TeacherId, t.Teacher.FullName, t.Teacher.Email))
                    .ToList(),
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
