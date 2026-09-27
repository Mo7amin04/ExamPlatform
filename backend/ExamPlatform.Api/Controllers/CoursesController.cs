using ExamPlatform.Api.Common;
using ExamPlatform.Api.Contracts;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Courses;
using ExamPlatform.Application.Features.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/courses")]
[Authorize(Policy = Policies.Staff)]
public sealed class CoursesController : ApiControllerBase
{
    /// <summary>Lists courses visible to the caller (teachers: assigned courses only).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CourseDto>>>> List(
        [FromQuery] Guid? departmentId, [FromQuery] string? search, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetCoursesQuery(departmentId, search), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDetailDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetCourseByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCourseCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command, cancellationToken), "Course created successfully.");

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDetailDto>>> Update(Guid id, UpdateCourseCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command with { Id = id }, cancellationToken), "Course updated successfully.");

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteCourseCommand(id), cancellationToken);
        return ApiResponse.Ok("Course deleted successfully.");
    }

    [HttpPost("{id:guid}/teachers")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> AssignTeacher(Guid id, AssignTeacherRequest request, CancellationToken cancellationToken)
    {
        await Sender.Send(new AssignTeacherCommand(id, request.TeacherId), cancellationToken);
        return ApiResponse.Ok("Teacher assigned to course.");
    }

    [HttpDelete("{id:guid}/teachers/{teacherId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> RemoveTeacher(Guid id, Guid teacherId, CancellationToken cancellationToken)
    {
        await Sender.Send(new RemoveTeacherCommand(id, teacherId), cancellationToken);
        return ApiResponse.Ok("Teacher removed from course.");
    }

    [HttpGet("{id:guid}/topics")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopicDto>>>> Topics(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetTopicsQuery(id), cancellationToken));

    [HttpPost("{id:guid}/topics")]
    [ProducesResponseType<ApiResponse<TopicDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTopic(Guid id, CreateTopicCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command with { CourseId = id }, cancellationToken), "Topic created successfully.");
}
