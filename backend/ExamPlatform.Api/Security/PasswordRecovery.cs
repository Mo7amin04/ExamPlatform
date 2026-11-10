using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using ExamPlatform.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExamPlatform.Api.Security;

public sealed class PasswordRecoveryOptions
{
    public string ResetPageUrl { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string FromAddress { get; set; } = "";
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost) && SmtpPort is > 0 and <= 65535
        && MailAddress.TryCreate(FromAddress, out _)
        && Uri.TryCreate(ResetPageUrl, UriKind.Absolute, out var url)
        && (url.Scheme == Uri.UriSchemeHttps || (url.Scheme == Uri.UriSchemeHttp && url.IsLoopback))
        && url.Fragment.Length == 0 && url.UserInfo.Length == 0;
}

public sealed class PasswordRecovery(
    IServiceScopeFactory scopes, IOptions<PasswordRecoveryOptions> options, PasswordResetTokens tokens,
    IPasswordResetMailer mailer, IMemoryCache cache, ILogger<PasswordRecovery> logger) : BackgroundService
{
    private readonly Channel<string> _requests = Channel.CreateBounded<string>(64);

    // Every valid address enters the same queue. SMTP delivery and account lookup are outside the HTTP response.
    public bool TryRequest(string email) => options.Value.IsConfigured && _requests.Writer.TryWrite(email.Trim().ToLowerInvariant());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _requests.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendLink(email, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Password reset delivery failed ({ErrorType}).", ex.GetType().Name);
            }
        }
    }

    private async Task SendLink(string email, CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var user = await db.Users.Where(u => u.Email == email && u.IsActive)
            .Select(u => new { u.Id, u.Email, u.PasswordHash }).SingleOrDefaultAsync(stoppingToken);
        if (user is null) return;

        var cooldownKey = (typeof(PasswordRecovery), user.Id);
        if (cache.TryGetValue(cooldownKey, out _)) return;
        cache.Set(cooldownKey, true, TimeSpan.FromMinutes(1));

        var settings = options.Value;
        var link = new UriBuilder(settings.ResetPageUrl) { Fragment = "token=" + Uri.EscapeDataString(tokens.Create(user.Id, user.PasswordHash)) }.Uri;
        await mailer.SendAsync(user.Email, link, stoppingToken);
    }
}

public interface IPasswordResetMailer
{
    Task SendAsync(string recipient, Uri link, CancellationToken cancellationToken);
}

public sealed class SmtpPasswordResetMailer(IOptions<PasswordRecoveryOptions> options) : IPasswordResetMailer
{
    public async Task SendAsync(string recipient, Uri link, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var message = new MailMessage(settings.FromAddress, recipient,
            "Exam Platform password reset | إعادة تعيين كلمة المرور",
            $"Use this link to reset your password within 30 minutes.\nاستخدم هذا الرابط لإعادة تعيين كلمة مرورك خلال 30 دقيقة.\n\n{link.AbsoluteUri}\n\nIf you did not request this, ignore this message.\nإذا لم تطلب إعادة التعيين، تجاهل هذه الرسالة.");
        using var smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort) { EnableSsl = true };
        if (!string.IsNullOrEmpty(settings.SmtpUsername))
            smtp.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await smtp.SendMailAsync(message, timeout.Token);
    }
}
