using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPlatform.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.HasIndex(d => d.Code).IsUnique();
    }
}

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.Property(c => c.Code).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.DepartmentId);

        builder.HasOne(c => c.Department)
            .WithMany(d => d.Courses)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourseTeacherConfiguration : IEntityTypeConfiguration<CourseTeacher>
{
    public void Configure(EntityTypeBuilder<CourseTeacher> builder)
    {
        builder.ToTable("CourseTeachers");
        builder.HasIndex(ct => new { ct.CourseId, ct.TeacherId }).IsUnique();
        builder.HasIndex(ct => ct.TeacherId);

        builder.HasOne(ct => ct.Course)
            .WithMany(c => c.CourseTeachers)
            .HasForeignKey(ct => ct.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Teacher)
            .WithMany(u => u.CourseAssignments)
            .HasForeignKey(ct => ct.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.HasIndex(t => new { t.CourseId, t.Name }).IsUnique();

        builder.HasOne(t => t.Course)
            .WithMany(c => c.Topics)
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
