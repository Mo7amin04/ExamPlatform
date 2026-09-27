using ExamPlatform.Api.Common;
using ExamPlatform.Api.Contracts;
using ExamPlatform.Api.Security;
using ExamPlatform.Application.Features.Exams;
using ExamPlatform.Application.Features.Exams.Preview;
using ExamPlatform.Application.Features.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamPlatform.Api.Controllers;

[Route("api/exams")]
[Authorize(Policy = Policies.Staff)]
public sealed class ExamsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<ExamSummaryDto>>> List([FromQuery] GetExamsQuery query, CancellationToken cancellationToken) =>
        Paged(await Sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetExamByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApiResponse<ExamDetailDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateExamCommand command, CancellationToken cancellationToken) =>
        CreatedResponse(await Sender.Send(command, cancellationToken), "Exam created successfully.");

    /// <summary>Saves exam settings (draft save).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> Update(Guid id, UpdateExamCommand command, CancellationToken cancellationToken) =>
        Success(await Sender.Send(command with { Id = id }, cancellationToken), "Exam saved.");

    /// <summary>Deletes a never-published exam, or archives an exam with published history.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteExamResult>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteExamCommand(id), cancellationToken);
        return Success(result, result.Archived ? "The exam has published history, so it was archived." : "Exam deleted successfully.");
    }

    // ----- Questions -----

    [HttpPost("{id:guid}/questions")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> AddQuestions(Guid id, AddExamQuestionsRequest request, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new AddExamQuestionsCommand(id, request.QuestionIds, request.Section, request.Points), cancellationToken),
            request.QuestionIds.Count == 1 ? "Question added to exam." : $"{request.QuestionIds.Count} questions added to exam.");

    [HttpPut("{id:guid}/questions/order")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> Reorder(Guid id, ReorderExamQuestionsRequest request, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new ReorderExamQuestionsCommand(id, request.QuestionIds), cancellationToken), "Question order saved.");

    [HttpPut("{id:guid}/questions/{questionId:guid}")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> UpdateQuestion(
        Guid id, Guid questionId, UpdateExamQuestionRequest request, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new UpdateExamQuestionCommand(id, questionId, request.Points, request.Section), cancellationToken), "Question updated.");

    [HttpDelete("{id:guid}/questions/{questionId:guid}")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> RemoveQuestion(Guid id, Guid questionId, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new RemoveExamQuestionCommand(id, questionId), cancellationToken), "Question removed from exam.");

    // ----- Status -----

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> Publish(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new PublishExamCommand(id), cancellationToken), "Exam published.");

    [HttpPost("{id:guid}/ready")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> MarkReady(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new MarkExamReadyCommand(id), cancellationToken), "Exam marked as ready.");

    /// <summary>Explicitly moves a ready/published exam back to draft so it can be edited.</summary>
    [HttpPost("{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse<ExamDetailDto>>> MoveToDraft(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new MoveExamToDraftCommand(id), cancellationToken), "Exam moved back to draft.");

    // ----- Preview -----

    /// <summary>Student-facing preview. Never contains correct answers or internal ids.</summary>
    [HttpGet("{id:guid}/preview")]
    public async Task<ActionResult<ApiResponse<ExamPreviewDto>>> Preview(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetExamPreviewQuery(id), cancellationToken));

    [HttpGet("{id:guid}/answer-key")]
    public async Task<ActionResult<ApiResponse<ExamAnswerKeyDto>>> AnswerKey(Guid id, CancellationToken cancellationToken) =>
        Success(await Sender.Send(new GetExamAnswerKeyQuery(id), cancellationToken));

    // ----- Export -----

    [HttpGet("{id:guid}/export/pdf")]
    [Produces("application/pdf")]
    public Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken) =>
        Export(id, ExportFormat.Pdf, ExportDocument.Exam, cancellationToken);

    [HttpGet("{id:guid}/export/word")]
    [Produces(ExportExamQueryHandler.DocxContentType)]
    public Task<IActionResult> ExportWord(Guid id, CancellationToken cancellationToken) =>
        Export(id, ExportFormat.Word, ExportDocument.Exam, cancellationToken);

    [HttpGet("{id:guid}/answer-key/pdf")]
    [Produces("application/pdf")]
    public Task<IActionResult> AnswerKeyPdf(Guid id, CancellationToken cancellationToken) =>
        Export(id, ExportFormat.Pdf, ExportDocument.AnswerKey, cancellationToken);

    [HttpGet("{id:guid}/answer-key/word")]
    [Produces(ExportExamQueryHandler.DocxContentType)]
    public Task<IActionResult> AnswerKeyWord(Guid id, CancellationToken cancellationToken) =>
        Export(id, ExportFormat.Word, ExportDocument.AnswerKey, cancellationToken);

    private async Task<IActionResult> Export(Guid id, ExportFormat format, ExportDocument document, CancellationToken cancellationToken)
    {
        var file = await Sender.Send(new ExportExamQuery(id, format, document), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
