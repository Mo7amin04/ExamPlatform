using System.Text.Json.Serialization;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Models;

namespace ExamPlatform.Api.Common;

/// <summary>Consistent envelope for every JSON response.</summary>
public class ApiResponse
{
    [JsonPropertyOrder(-10)]
    public bool Success { get; init; }

    [JsonPropertyOrder(-9)]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyOrder(10)]
    public IReadOnlyList<FieldError> Errors { get; init; } = [];

    public static ApiResponse Ok(string message = "") => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<FieldError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [] };
}

public class ApiResponse<T> : ApiResponse
{
    [JsonPropertyOrder(0)]
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "") => new() { Success = true, Message = message, Data = data };
}

public sealed record PaginationMeta(int Page, int PageSize, int TotalCount, int TotalPages);

public sealed class PagedApiResponse<T> : ApiResponse<IReadOnlyList<T>>
{
    [JsonPropertyOrder(1)]
    public PaginationMeta Pagination { get; init; } = new(1, 0, 0, 0);

    public static PagedApiResponse<T> From(PagedResult<T> result, string message = "") => new()
    {
        Success = true,
        Message = message,
        Data = result.Items,
        Pagination = new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages)
    };
}
