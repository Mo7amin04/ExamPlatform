using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExamPlatform.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.Property(q => q.Text).HasMaxLength(4000).IsRequired();
        builder.Property(q => q.Explanation).HasMaxLength(4000);
        builder.Property(q => q.ExpectedAnswer).HasMaxLength(4000);
        builder.Ignore(q => q.IsArchived);

        builder.HasIndex(q => q.CourseId);
        builder.HasIndex(q => q.TopicId);
        builder.HasIndex(q => q.Type);
        builder.HasIndex(q => q.Difficulty);
        builder.HasIndex(q => q.BloomLevel);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => new { q.CourseId, q.Status });

        builder.HasOne(q => q.Course)
            .WithMany(c => c.Questions)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict avoids multiple cascade paths (Course -> Topics -> Questions); handlers detach questions first.
        builder.HasOne(q => q.Topic)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");
        builder.Property(o => o.Text).HasMaxLength(1000).IsRequired();
        builder.Property(o => o.MatchText).HasMaxLength(1000);
        builder.HasIndex(o => new { o.QuestionId, o.Order });

        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.Property(t => t.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public sealed class QuestionTagConfiguration : IEntityTypeConfiguration<QuestionTag>
{
    public void Configure(EntityTypeBuilder<QuestionTag> builder)
    {
        builder.ToTable("QuestionTags");
        builder.HasKey(qt => new { qt.QuestionId, qt.TagId });
        builder.HasIndex(qt => qt.TagId);

        builder.HasOne(qt => qt.Question).WithMany(q => q.QuestionTags).HasForeignKey(qt => qt.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(qt => qt.Tag).WithMany(t => t.QuestionTags).HasForeignKey(qt => qt.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
