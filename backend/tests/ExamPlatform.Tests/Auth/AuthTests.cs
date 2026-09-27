using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExamPlatform.Tests.Auth;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<string> LoginAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = ApiFactory.Password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task ValidLogin_ReturnsTokenAndUserInStandardEnvelope()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = ApiFactory.TeacherEmail.ToUpperInvariant(), password = ApiFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("data").GetProperty("accessToken").GetString()));
        Assert.Equal("Teacher", json.GetProperty("data").GetProperty("user").GetProperty("roles")[0].GetString());
    }

    [Fact]
    public async Task InvalidLogin_Returns401WithoutRevealingWhichFieldWasWrong()
    {
        var client = factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email = ApiFactory.TeacherEmail, password = "nope-nope" });
        var unknownUser = await client.PostAsJsonAsync("/api/auth/login", new { email = "ghost@test.local", password = "nope-nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        var a = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var b = await unknownUser.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(a.GetProperty("message").GetString(), b.GetProperty("message").GetString());
    }

    [Fact]
    public async Task LoginValidation_Returns400WithFieldErrors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "not-an-email", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("errors").GetArrayLength() >= 2);
    }

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/questions")]
    [InlineData("/api/exams")]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var response = await ClientWithToken("not.a.jwt").GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsIdentityDerivedFromToken()
    {
        var token = await LoginAsync(ApiFactory.AdminEmail);

        var json = await ClientWithToken(token).GetFromJsonAsync<JsonElement>("/api/auth/me");

        Assert.Equal(ApiFactory.AdminEmail, json.GetProperty("data").GetProperty("email").GetString());
    }

    [Fact]
    public async Task TeacherCallingAdminEndpoint_Returns403()
    {
        var token = await LoginAsync(ApiFactory.TeacherEmail);

        var createDepartment = await ClientWithToken(token).PostAsJsonAsync("/api/departments", new { name = "Physics", code = "PHY" });
        var listUsers = await ClientWithToken(token).GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, createDepartment.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, listUsers.StatusCode);
    }

    [Fact]
    public async Task AdminCallingAdminEndpoint_Succeeds()
    {
        var token = await LoginAsync(ApiFactory.AdminEmail);

        var response = await ClientWithToken(token).PostAsJsonAsync("/api/departments", new { name = "Mathematics", code = "MATH" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TeacherAccessingUnassignedCourse_Returns403()
    {
        var token = await LoginAsync(ApiFactory.TeacherEmail);

        var response = await ClientWithToken(token).GetAsync($"/api/courses/{factory.OtherCourseId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
