using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExamPlatform.Api.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExamPlatform.Tests.Auth;

public sealed class PasswordRecoveryTests
{
    [Fact]
    public async Task EmailedLink_ResetsPasswordOnceWithoutRevealingWhichAccountsExist()
    {
        var mailer = new CapturingMailer();
        using var root = new ApiFactory();
        using var factory = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.PostConfigure<PasswordRecoveryOptions>(options =>
            {
                options.ResetPageUrl = "https://school.example/app/reset-password";
                options.FromAddress = "recovery@school.example";
                options.SmtpHost = "smtp.school.example";
            });
            services.Replace(ServiceDescriptor.Singleton<IPasswordResetMailer>(mailer));
        }));
        using var client = factory.CreateClient();
        const string forgot = "/api/auth/forgot-password";
        const string reset = "/api/auth/reset-password";
        const string login = "/api/auth/login";
        const string newPassword = "Recovered@67890";
        var email = ApiFactory.TeacherEmail;

        using var oldSession = await client.PostAsJsonAsync(login, new { email, password = ApiFactory.Password });
        oldSession.EnsureSuccessStatusCode();
        var json = await oldSession.Content.ReadFromJsonAsync<JsonElement>();
        var oldToken = json.GetProperty("data").GetProperty("accessToken").GetString();

        using var unknown = await client.PostAsJsonAsync(forgot, new { email = "unknown@test.local" });
        using var known = await client.PostAsJsonAsync(forgot, new { email = email.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, known.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
        var (recipient, link) = await mailer.Delivery.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(email, recipient);
        Assert.Equal("https://school.example/app/reset-password", link.GetLeftPart(UriPartial.Path));
        Assert.StartsWith("#token=", link.Fragment);
        var token = Uri.UnescapeDataString(link.Fragment[7..]);

        using var forged = await client.PostAsJsonAsync(reset, new { token = "invalid", newPassword });
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        using var completed = await client.PostAsJsonAsync(reset, new { token, newPassword });
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
        using var replay = await client.PostAsJsonAsync(reset, new { token, newPassword = "Replayed@12345" });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);

        using var oldPassword = await client.PostAsJsonAsync(login, new { email, password = ApiFactory.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        using var signedIn = await client.PostAsJsonAsync(login, new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
        using var me = await client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    private sealed class CapturingMailer : IPasswordResetMailer
    {
        public TaskCompletionSource<(string Recipient, Uri Link)> Delivery { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(string recipient, Uri link, CancellationToken cancellationToken)
        {
            Delivery.TrySetResult((recipient, link));
            return Task.CompletedTask;
        }
    }
}
