using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPlatform.Infrastructure.Persistence.Configurations;

public sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams");
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Instructions).HasMaxLength(4000);
        builder.Property(e => e.TotalPoints);
        builder.Property(e => e.Status);
        builder.Ignore(e => e.IsEditable);

        builder.HasIndex(e => e.CourseId);
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Exams)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("ExamQuestions");
        builder.Property(eq => eq.Section).HasMaxLength(200);

        builder.HasIndex(eq => eq.ExamId);
        builder.HasIndex(eq => eq.QuestionId);
        builder.HasIndex(eq => new { eq.ExamId, eq.QuestionId }).IsUnique();

        builder.HasOne(eq => eq.Exam)
            .WithMany(e => e.Questions)
            .HasForeignKey(eq => eq.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(eq => eq.Question)
            .WithMany(q => q.ExamQuestions)
            .HasForeignKey(eq => eq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamVersionConfiguration : IEntityTypeConfiguration<ExamVersion>
{
    public void Configure(EntityTypeBuilder<ExamVersion> builder)
    {
        builder.ToTable("ExamVersions");
        builder.Property(v => v.Title).HasMaxLength(300).IsRequired();
        builder.HasIndex(v => new { v.ExamId, v.VersionNumber }).IsUnique();

        builder.HasOne(v => v.Exam)
            .WithMany(e => e.Versions)
            .HasForeignKey(v => v.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ExamVersionQuestionConfiguration : IEntityTypeConfiguration<ExamVersionQuestion>
{
    public void Configure(EntityTypeBuilder<ExamVersionQuestion> builder)
    {
        builder.ToTable("ExamVersionQuestions");
        builder.Property(vq => vq.Section).HasMaxLength(200);
        builder.HasIndex(vq => new { vq.ExamVersionId, vq.QuestionId }).IsUnique();
        builder.HasIndex(vq => vq.QuestionId);

        builder.HasOne(vq => vq.ExamVersion)
            .WithMany(v => v.Questions)
            .HasForeignKey(vq => vq.ExamVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vq => vq.Question)
            .WithMany()
            .HasForeignKey(vq => vq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AIGenerationConfiguration : IEntityTypeConfiguration<AIGeneration>
{
    public void Configure(EntityTypeBuilder<AIGeneration> builder)
    {
        builder.ToTable("AIGenerations");
        builder.Property(g => g.Provider).HasMaxLength(50).IsRequired();
        builder.Property(g => g.Model).HasMaxLength(100);
        builder.Property(g => g.RequestPayload).IsRequired();
        builder.Property(g => g.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(g => g.CourseId);
        builder.HasIndex(g => g.CreatedBy);

        builder.HasOne(g => g.Course)
            .WithMany()
            .HasForeignKey(g => g.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Topic)
            .WithMany()
            .HasForeignKey(g => g.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
