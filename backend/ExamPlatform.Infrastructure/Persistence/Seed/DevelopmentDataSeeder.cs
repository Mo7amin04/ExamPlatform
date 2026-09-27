using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only seed configuration ("Seed" section). The password is read from configuration
/// (appsettings.Development.json / user-secrets) and is never hard-coded.
/// </summary>
public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
    public string DefaultPassword { get; set; } = string.Empty;
    public string EmailDomain { get; set; } = "exam-platform.local";
}

/// <summary>Seeds demo users, departments, courses, topics, questions and exams into an empty database.</summary>
public sealed class DevelopmentDataSeeder(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedSettings> options,
    ILogger<DevelopmentDataSeeder> logger)
{
    private readonly SeedSettings _settings = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(_settings.DefaultPassword))
        {
            logger.LogWarning("Seed:Enabled is true but Seed:DefaultPassword is empty; skipping development seed.");
            return;
        }

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Seeding development data...");

        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin, cancellationToken);
        var teacherRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Teacher, cancellationToken);

        var admin = CreateUser("System Administrator", "admin", adminRole);
        var sarah = CreateUser("Dr. Sarah Ahmed", "sarah.ahmed", teacherRole);
        var omar = CreateUser("Dr. Omar Hassan", "omar.hassan", teacherRole);
        db.Users.AddRange(admin, sarah, omar);

        var cs = new Department { Name = "Computer Science", Code = "CS", Description = "Department of Computer Science" };
        var math = new Department { Name = "Mathematics", Code = "MATH", Description = "Department of Mathematics" };
        db.Departments.AddRange(cs, math);

        var cs101 = CreateCourse(cs, "CS101", "Introduction to Programming", "Fundamentals of programming using Python.", 3, sarah,
            ("Variables and Data Types", "Primitive types, variables, expressions"),
            ("Control Flow", "Conditionals and loops"),
            ("Functions", "Defining and calling functions, scope, recursion"));
        var cs201 = CreateCourse(cs, "CS201", "Data Structures", "Core data structures and their complexity.", 3, sarah,
            ("Arrays and Linked Lists", null),
            ("Stacks and Queues", null),
            ("Trees", "Binary trees, BSTs and traversals"),
            ("Algorithm Complexity", "Big-O notation"));
        var cs301 = CreateCourse(cs, "CS301", "Database Systems", "Relational databases, SQL and design.", 3, omar,
            ("Relational Model", null),
            ("SQL", "Queries, joins and aggregation"),
            ("Normalization", "Functional dependencies and normal forms"));
        var math201 = CreateCourse(math, "MATH201", "Linear Algebra", "Vectors, matrices and linear transformations.", 3, omar,
            ("Vectors", null),
            ("Matrices", null),
            ("Eigenvalues and Eigenvectors", null));
        db.Courses.AddRange(cs101, cs201, cs301, math201);

        var questions = new List<Question>
        {
            // CS101
            Mc(cs101, "Variables and Data Types", "Which of the following is an immutable data type in Python?", Difficulty.Easy, BloomLevel.Remember, 1,
                "Tuples cannot be modified after creation.", ("list", false), ("dict", false), ("tuple", true), ("set", false)),
            Tf(cs101, "Variables and Data Types", "In Python, the expression 7 // 2 evaluates to 3.", true, Difficulty.Easy, BloomLevel.Understand, 1,
                "// performs floor division."),
            Mc(cs101, "Control Flow", "How many times does the body of `for i in range(2, 10, 3):` execute?", Difficulty.Medium, BloomLevel.Apply, 2,
                "The loop visits 2, 5 and 8.", ("2", false), ("3", true), ("4", false), ("8", false)),
            Short(cs101, "Functions", "What is the term for a function that calls itself?", "Recursion (a recursive function)", Difficulty.Easy, BloomLevel.Remember, 1),
            Essay(cs101, "Functions", "Explain the difference between local and global scope, and describe one situation where using a global variable is a poor design choice.",
                "Defines local vs global scope (3); explains lifetime/visibility (2); gives a reasoned example of harm, e.g. hidden coupling or testing difficulty (3); clarity (2).",
                Difficulty.Medium, BloomLevel.Evaluate, 10),

            // CS201
            Mc(cs201, "Stacks and Queues", "Which data structure follows the Last-In, First-Out (LIFO) principle?", Difficulty.Easy, BloomLevel.Remember, 1,
                null, ("Queue", false), ("Stack", true), ("Heap", false), ("Linked list", false)),
            Mc(cs201, "Algorithm Complexity", "What is the worst-case time complexity of searching for a value in a balanced binary search tree with n nodes?",
                Difficulty.Medium, BloomLevel.Understand, 2, "The height of a balanced BST is O(log n).",
                ("O(1)", false), ("O(log n)", true), ("O(n)", false), ("O(n log n)", false)),
            Ms(cs201, "Arrays and Linked Lists", "Which of the following operations are O(1) on a singly linked list with a head pointer?",
                Difficulty.Medium, BloomLevel.Analyze, 3, ("Insert at head", true), ("Delete head", true), ("Access the k-th element", false), ("Insert at tail without a tail pointer", false)),
            Order(cs201, "Trees", "Arrange the nodes in the order an in-order traversal visits them for the BST built by inserting 50, 30, 70, 20, 40.",
                Difficulty.Medium, BloomLevel.Apply, 3, "20", "30", "40", "50", "70"),
            Match(cs201, "Algorithm Complexity", "Match each operation with its average time complexity.", Difficulty.Medium, BloomLevel.Understand, 4,
                ("Hash table lookup", "O(1)"), ("Binary search", "O(log n)"), ("Linear search", "O(n)"), ("Merge sort", "O(n log n)")),
            Fill(cs201, "Stacks and Queues", "The operation that removes the front element of a queue is called _____.", "dequeue", Difficulty.Easy, BloomLevel.Remember, 1),

            // CS301
            Mc(cs301, "SQL", "Which SQL clause filters groups produced by GROUP BY?", Difficulty.Easy, BloomLevel.Remember, 1,
                "WHERE filters rows before grouping; HAVING filters groups.", ("WHERE", false), ("HAVING", true), ("ORDER BY", false), ("LIMIT", false)),
            Mc(cs301, "Normalization", "A relation is in 2NF when it is in 1NF and…", Difficulty.Medium, BloomLevel.Understand, 2, null,
                ("it has no transitive dependencies", false),
                ("every non-key attribute is fully functionally dependent on the whole primary key", true),
                ("every determinant is a candidate key", false),
                ("it has no multivalued dependencies", false)),
            Tf(cs301, "Relational Model", "A foreign key value may be NULL unless a NOT NULL constraint is specified.", true, Difficulty.Medium, BloomLevel.Understand, 1, null),
            Short(cs301, "SQL", "Write a SQL query that returns the number of students in each department from table Students(id, name, department_id).",
                "SELECT department_id, COUNT(*) FROM Students GROUP BY department_id;", Difficulty.Medium, BloomLevel.Apply, 4),

            // MATH201
            Mc(math201, "Matrices", "If A is a 3×2 matrix and B is a 2×4 matrix, what is the size of AB?", Difficulty.Easy, BloomLevel.Apply, 1, null,
                ("2×2", false), ("3×4", true), ("4×3", false), ("AB is undefined", false)),
            Tf(math201, "Eigenvalues and Eigenvectors", "Every square matrix with real entries has at least one real eigenvalue.", false,
                Difficulty.Hard, BloomLevel.Analyze, 2, "A 2×2 rotation matrix by 90° has only complex eigenvalues."),
            Short(math201, "Vectors", "Compute the dot product of u = (1, 2, 3) and v = (4, -5, 6).", "12", Difficulty.Easy, BloomLevel.Apply, 2),
        };

        // One AI-sourced draft to demonstrate the review workflow.
        var aiDraft = Mc(cs201, "Trees", "What is the maximum number of nodes in a binary tree of height 3 (root at height 0)?",
            Difficulty.Medium, BloomLevel.Apply, 2, "A perfect binary tree of height h has 2^(h+1) − 1 nodes.",
            ("7", false), ("8", false), ("15", true), ("16", false));
        aiDraft.Source = QuestionSource.AI;
        aiDraft.Status = QuestionStatus.Draft;
        questions.Add(aiDraft);

        db.Questions.AddRange(questions);

        var dataStructuresMidterm = new Exam
        {
            Course = cs201,
            CourseId = cs201.Id,
            Title = "Data Structures — Midterm Exam",
            Description = "Covers linked lists, stacks, queues, trees and complexity.",
            Instructions = "Answer all questions. Write clearly. Calculators are not permitted.",
            Type = ExamType.Midterm,
            DurationMinutes = 90,
            ExamDate = DateTime.UtcNow.Date.AddDays(14).AddHours(9)
        };
        foreach (var q in questions.Where(q => q.CourseId == cs201.Id && q.Status == QuestionStatus.Approved))
            dataStructuresMidterm.AddQuestion(q, section: q.Type is QuestionType.MultipleChoice or QuestionType.MultipleSelect
                ? "Part A — Multiple choice" : "Part B — Written answers");
        db.Exams.Add(dataStructuresMidterm);

        var programmingQuiz = new Exam
        {
            Course = cs101,
            CourseId = cs101.Id,
            Title = "Quiz 1 — Python Basics",
            Type = ExamType.Quiz,
            DurationMinutes = 20,
            ExamDate = DateTime.UtcNow.Date.AddDays(-7).AddHours(10)
        };
        foreach (var q in questions.Where(q => q.CourseId == cs101.Id && q.Type != QuestionType.Essay))
            programmingQuiz.AddQuestion(q);
        db.ExamVersions.Add(programmingQuiz.Publish(1));
        db.Exams.Add(programmingQuiz);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development data seeded: {Users} users, {Courses} courses, {Questions} questions.",
            3, 4, questions.Count);
    }

    private User CreateUser(string fullName, string localPart, Role role)
    {
        var user = new User
        {
            FullName = fullName,
            Email = $"{localPart}@{_settings.EmailDomain}".ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(_settings.DefaultPassword),
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        return user;
    }

    private static Course CreateCourse(Department department, string code, string name, string description, int credits,
        User teacher, params (string Name, string? Description)[] topics)
    {
        var course = new Course
        {
            Department = department,
            DepartmentId = department.Id,
            Code = code,
            Name = name,
            Description = description,
            CreditHours = credits
        };
        course.CourseTeachers.Add(new CourseTeacher { CourseId = course.Id, TeacherId = teacher.Id });
        var order = 1;
        foreach (var (topicName, topicDescription) in topics)
            course.Topics.Add(new Topic { CourseId = course.Id, Name = topicName, Description = topicDescription, Order = order++ });
        return course;
    }

    private static Question NewQuestion(Course course, string topic, string text, QuestionType type, Difficulty difficulty,
        BloomLevel bloom, decimal points, string? explanation = null, string? expected = null) => new()
    {
        CourseId = course.Id,
        TopicId = course.Topics.First(t => t.Name == topic).Id,
        Text = text,
        Type = type,
        Difficulty = difficulty,
        BloomLevel = bloom,
        Points = points,
        Explanation = explanation,
        ExpectedAnswer = expected,
        Status = QuestionStatus.Approved,
        Source = QuestionSource.Manual
    };

    private static Question WithOptions(Question question, IEnumerable<(string Text, bool IsCorrect, string? Match)> options)
    {
        var order = 1;
        foreach (var (text, isCorrect, match) in options)
            question.Options.Add(new QuestionOption { QuestionId = question.Id, Text = text, IsCorrect = isCorrect, MatchText = match, Order = order++ });
        return question;
    }

    private static Question Mc(Course c, string topic, string text, Difficulty d, BloomLevel b, decimal pts, string? explanation,
        params (string Text, bool IsCorrect)[] options) =>
        WithOptions(NewQuestion(c, topic, text, QuestionType.MultipleChoice, d, b, pts, explanation), options.Select(o => (o.Text, o.IsCorrect, (string?)null)));

    private static Question Ms(Course c, string topic, string text, Difficulty d, BloomLevel b, decimal pts,
        params (string Text, bool IsCorrect)[] options) =>
        WithOptions(NewQuestion(c, topic, text, QuestionType.MultipleSelect, d, b, pts), options.Select(o => (o.Text, o.IsCorrect, (string?)null)));

    private static Question Tf(Course c, string topic, string text, bool answer, Difficulty d, BloomLevel b, decimal pts, string? explanation) =>
        WithOptions(NewQuestion(c, topic, text, QuestionType.TrueFalse, d, b, pts, explanation),
            [("True", answer, null), ("False", !answer, null)]);

    private static Question Short(Course c, string topic, string text, string expected, Difficulty d, BloomLevel b, decimal pts) =>
        NewQuestion(c, topic, text, QuestionType.ShortAnswer, d, b, pts, expected: expected);

    private static Question Fill(Course c, string topic, string text, string expected, Difficulty d, BloomLevel b, decimal pts) =>
        NewQuestion(c, topic, text, QuestionType.FillBlank, d, b, pts, expected: expected);

    private static Question Essay(Course c, string topic, string text, string rubric, Difficulty d, BloomLevel b, decimal pts) =>
        NewQuestion(c, topic, text, QuestionType.Essay, d, b, pts, expected: rubric);

    private static Question Order(Course c, string topic, string text, Difficulty d, BloomLevel b, decimal pts, params string[] items) =>
        WithOptions(NewQuestion(c, topic, text, QuestionType.Ordering, d, b, pts), items.Select(i => (i, false, (string?)null)));

    private static Question Match(Course c, string topic, string text, Difficulty d, BloomLevel b, decimal pts,
        params (string Left, string Right)[] pairs) =>
        WithOptions(NewQuestion(c, topic, text, QuestionType.Matching, d, b, pts), pairs.Select(p => (p.Left, false, (string?)p.Right)));
}
