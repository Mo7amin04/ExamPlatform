namespace ExamPlatform.Domain.Exceptions;

/// <summary>
/// Raised when a domain invariant or business rule is violated.
/// </summary>
public class DomainException(string message) : Exception(message);
