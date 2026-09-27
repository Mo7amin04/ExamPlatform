using ExamPlatform.Domain.Common;

namespace ExamPlatform.Domain.Entities;

public class Department : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public class Course : AuditableEntity
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

public class CourseTeacher : AuditableEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
}

public class Topic : AuditableEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
