using FluentValidation.Results;

namespace ExamPlatform.Application.Common.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string entityName, object key)
        : this($"{entityName} '{key}' was not found.")
    {
    }
}

public class ForbiddenAccessException(string message = "You do not have permission to perform this action.")
    : Exception(message);

public class UnauthorizedException(string message = "Authentication is required.") : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .Select(f => new FieldError(f.PropertyName, f.ErrorMessage))
            .Distinct()
            .ToList();
    }

    public ValidationException(string field, string message)
        : base("One or more validation errors occurred.")
    {
        Errors = [new FieldError(field, message)];
    }

    public IReadOnlyList<FieldError> Errors { get; }
}

public sealed record FieldError(string? Field, string Message);

/// <summary>Raised when the configured AI provider fails or returns an unusable response.</summary>
public class AIProviderException(string message, bool isConfigurationError = false, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsConfigurationError { get; } = isConfigurationError;
}
