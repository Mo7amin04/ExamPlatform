using System.Reflection;
using ExamPlatform.Application.Common.Behaviors;
using ExamPlatform.Application.Common.Security;
using ExamPlatform.Application.Common.Settings;
using ExamPlatform.Application.Features.AI;
using ExamPlatform.Application.Features.Questions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.Configure<InstitutionSettings>(configuration.GetSection(InstitutionSettings.SectionName));
        services.Configure<FileUploadSettings>(configuration.GetSection(FileUploadSettings.SectionName));

        services.AddScoped<ICourseAccessService, CourseAccessService>();
        services.AddScoped<QuestionWriter>();
        services.AddScoped<AIGenerationLogger>();

        return services;
    }
}
