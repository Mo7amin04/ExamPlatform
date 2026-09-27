using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Features.Questions;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using ExamPlatform.Infrastructure.Persistence;
using ExamPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Tests.Common;

public sealed class TestCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public HashSet<string> Roles { get; } = [];
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsInRole(string role) => Roles.Contains(role);
}

/// <summary>
/// In-memory application context with a small academic fixture:
/// one department, two courses (teacher assigned only to <see cref="Course"/>), a topic and a teacher user.
/// </summary>
public sealed class TestContext : IDisposable
{
    public ApplicationDbContext Db { get; }
    public TestCurrentUser CurrentUser { get; } = new();
    public ICourseAccessService Access { get; }

    public User Teacher { get; }
    public Department Department { get; }
    public Course Course { get; }
    public Course OtherCourse { get; }
    public Topic Topic { get; }

    public TestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"tests-{Guid.NewGuid()}")
            .AddInterceptors(new AuditableEntityInterceptor(CurrentUser, TimeProvider.System))
            .Options;
        Db = new ApplicationDbContext(options);

        var teacherRole = new Role { Id = RoleNames.TeacherRoleId, Name = RoleNames.Teacher };
        Teacher = new User { FullName = "Test Teacher", Email = "teacher@test.local", PasswordHash = "x" };
        Teacher.UserRoles.Add(new UserRole { UserId = Teacher.Id, RoleId = teacherRole.Id, Role = teacherRole });

        Department = new Department { Name = "Computer Science", Code = "CS" };
        Course = new Course { Department = Department, DepartmentId = Department.Id, Code = "CS101", Name = "Programming" };
        OtherCourse = new Course { Department = Department, DepartmentId = Department.Id, Code = "CS999", Name = "Other" };
        Topic = new Topic { CourseId = Course.Id, Name = "Loops" };
        Course.Topics.Add(Topic);
        Course.CourseTeachers.Add(new CourseTeacher { CourseId = Course.Id, TeacherId = Teacher.Id });

        Db.AddRange(teacherRole, Teacher, Department, Course, OtherCourse);
        Db.SaveChanges();

        CurrentUser.UserId = Teacher.Id;
        CurrentUser.Roles.Add(RoleNames.Teacher);
        Access = new CourseAccessService(Db, CurrentUser);
    }

    public QuestionWriter Writer => new(Db, Access);

    public Question AddQuestion(
        Course? course = null,
        string text = "What is 2 + 2?",
        QuestionType type = QuestionType.MultipleChoice,
        Difficulty difficulty = Difficulty.Easy,
        decimal points = 2,
        QuestionStatus status = QuestionStatus.Approved)
    {
        var question = new Question
        {
            CourseId = (course ?? Course).Id,
            Text = text,
            Type = type,
            Difficulty = difficulty,
            BloomLevel = BloomLevel.Remember,
            Points = points,
            Status = status
        };
        if (type == QuestionType.MultipleChoice)
        {
            question.Options.Add(new QuestionOption { QuestionId = question.Id, Text = "3", Order = 1 });
            question.Options.Add(new QuestionOption { QuestionId = question.Id, Text = "4", IsCorrect = true, Order = 2 });
        }
        Db.Questions.Add(question);
        Db.SaveChanges();
        return question;
    }

    public Exam AddExam(Course? course = null, int duration = 60)
    {
        var exam = new Exam { CourseId = (course ?? Course).Id, Title = "Midterm", Type = ExamType.Midterm, DurationMinutes = duration };
        Db.Exams.Add(exam);
        Db.SaveChanges();
        return exam;
    }

    public void Dispose() => Db.Dispose();
}
