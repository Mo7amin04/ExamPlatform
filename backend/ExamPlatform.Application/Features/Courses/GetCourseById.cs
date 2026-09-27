using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Topics;
using ExamPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Features.Courses;

public sealed record GetCourseByIdQuery(Guid Id) : IRequest<CourseDetailDto>;

public sealed class GetCourseByIdQueryHandler(IApplicationDbContext db, ICourseAccessService access)
    : IRequestHandler<GetCourseByIdQuery, CourseDetailDto>
{
    public async Task<CourseDetailDto> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        await access.EnsureCourseAccessAsync(request.Id, cancellationToken);

        return await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CourseDetailDto(
                c.Id,
                c.DepartmentId,
                c.Department.Name,
                c.Code,
                c.Name,
                c.Description,
                c.CreditHours,
                c.Questions.Count(q => q.Status != QuestionStatus.Archived),
                c.Exams.Count(e => e.Status != ExamStatus.Archived),
                c.CourseTeachers
                    .OrderBy(t => t.Teacher.FullName)
                    .Select(t => new CourseTeacherDto(t.TeacherId, t.Teacher.FullName, t.Teacher.Email))
                    .ToList(),
                c.Topics
                    .OrderBy(t => t.Order).ThenBy(t => t.Name)
                    .Select(t => new TopicDto(t.Id, t.CourseId, t.Name, t.Description, t.Order,
                        t.Questions.Count(q => q.Status != QuestionStatus.Archived)))
                    .ToList(),
                c.CreatedAt))
            .FirstAsync(cancellationToken);
    }
}
