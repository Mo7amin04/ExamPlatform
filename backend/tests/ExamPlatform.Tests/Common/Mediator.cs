using ExamPlatform.Application.Features.Exams;
using ExamPlatform.Application.Features.Questions;
using MediatR;

namespace ExamPlatform.Tests.Common;

/// <summary>
/// Minimal ISender for handler tests: resolves the follow-up read queries that command handlers dispatch
/// (e.g. "return the saved entity") against the same test context.
/// </summary>
public sealed class TestSender(TestContext ctx) : ISender
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        object result = request switch
        {
            GetQuestionByIdQuery q => await new GetQuestionByIdQueryHandler(ctx.Db, ctx.Access).Handle(q, cancellationToken),
            GetExamByIdQuery q => await new GetExamByIdQueryHandler(ctx.Db, ctx.Access).Handle(q, cancellationToken),
            _ => throw new NotSupportedException($"TestSender cannot handle {request.GetType().Name}.")
        };
        return (TResponse)result;
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        throw new NotSupportedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
