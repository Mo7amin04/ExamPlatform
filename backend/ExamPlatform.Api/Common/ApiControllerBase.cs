using ExamPlatform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Common;

/// <summary>Thin base controller: dispatches to MediatR and wraps results. No business logic lives here.</summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ApiResponse>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ApiResponse>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _sender;

    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected static ApiResponse<T> Success<T>(T data, string message = "") => ApiResponse<T>.Ok(data, message);

    protected static PagedApiResponse<T> Paged<T>(PagedResult<T> result) => PagedApiResponse<T>.From(result);

    protected ObjectResult CreatedResponse<T>(T data, string message) =>
        StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message));
}
