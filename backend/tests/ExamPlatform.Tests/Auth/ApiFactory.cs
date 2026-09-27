using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Domain.Constants;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Infrastructure.Persistence;
using ExamPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ExamPlatform.Tests.Auth;

/// <summary>Runs the real API pipeline (auth, policies, middleware) against an in-memory database.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Test@12345";
    public const string AdminEmail = "admin@test.local";
    public const string TeacherEmail = "teacher@test.local";
    public const string OtherTeacherEmail = "other@test.local";

    public Guid OtherCourseId { get; private set; }

    private readonly string _databaseName = $"api-tests-{Guid.NewGuid()}";

    static ApiFactory()
    {
        // Values read while Program.cs builds the host must be available before WebApplication.Build().
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-0123456789-abcdef");
        Environment.SetEnvironmentVariable("AI__Provider", "Mock");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=unused;Database=unused");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.AddDbContext<ApplicationDbContext>((sp, options) => options
                .UseInMemoryDatabase(_databaseName)
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        Seed(host.Services);
        return host;
    }

    private void Seed(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Database.EnsureCreated(); // applies HasData roles

        var admin = CreateUser("Admin", AdminEmail, RoleNames.AdminRoleId, hasher);
        var teacher = CreateUser("Teacher", TeacherEmail, RoleNames.TeacherRoleId, hasher);
        var other = CreateUser("Other Teacher", OtherTeacherEmail, RoleNames.TeacherRoleId, hasher);

        var department = new Department { Name = "CS", Code = "CS" };
        var otherCourse = new Course { DepartmentId = department.Id, Code = "CS900", Name = "Not yours" };
        otherCourse.CourseTeachers.Add(new CourseTeacher { CourseId = otherCourse.Id, TeacherId = other.Id });
        OtherCourseId = otherCourse.Id;

        db.AddRange(admin, teacher, other, department, otherCourse);
        db.SaveChanges();
    }

    private static User CreateUser(string name, string email, Guid roleId, IPasswordHasher hasher)
    {
        var user = new User { FullName = name, Email = email, PasswordHash = hasher.Hash(Password) };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        return user;
    }
}
