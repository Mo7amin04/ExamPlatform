using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction used by handlers. Implemented in Infrastructure by the EF Core DbContext;
/// the Application layer only depends on EF Core's provider-agnostic abstractions (DbSet / IQueryable).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Department> Departments { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseTeacher> CourseTeachers { get; }
    DbSet<Topic> Topics { get; }
    DbSet<Question> Questions { get; }
    DbSet<QuestionOption> QuestionOptions { get; }
    DbSet<Tag> Tags { get; }
    DbSet<QuestionTag> QuestionTags { get; }
    DbSet<Exam> Exams { get; }
    DbSet<ExamQuestion> ExamQuestions { get; }
    DbSet<ExamVersion> ExamVersions { get; }
    DbSet<ExamVersionQuestion> ExamVersionQuestions { get; }
    DbSet<AIGeneration> AIGenerations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
