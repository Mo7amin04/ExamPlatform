using System.Text.Json.Serialization;
using ExamPlatform.Api.Common;
using ExamPlatform.Api.Security;
using ExamPlatform.Application;
using ExamPlatform.Application.Common.Exceptions;
using ExamPlatform.Application.Common.Interfaces;
using ExamPlatform.Application.Common.Settings;
using ExamPlatform.Infrastructure;
using ExamPlatform.Infrastructure.Identity;
using ExamPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ----- Layers -----
builder.Services.AddApplication(configuration);
builder.Services.AddInfrastructure(configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ----- MVC / JSON -----
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // Model-binding failures use the same envelope as FluentValidation errors.
        o.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => new FieldError(
                    e.Key.StartsWith("$.") ? e.Key[2..] : e.Key,
                    string.IsNullOrWhiteSpace(err.ErrorMessage) ? "The value is invalid." : err.ErrorMessage)))
                .ToList();
            return new BadRequestObjectResult(ApiResponse.Fail("One or more validation errors occurred.", errors));
        };
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ----- Uploads -----
var uploadSettings = configuration.GetSection(FileUploadSettings.SectionName).Get<FileUploadSettings>() ?? new FileUploadSettings();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = uploadSettings.MaxFileSizeBytes + 1024 * 1024);

// ----- Authentication / Authorization -----
var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSettings.GetSecurityKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtTokenGenerator.NameClaimType,
            RoleClaimType = JwtTokenGenerator.RoleClaimType
        };
        options.Events = new JwtBearerEvents
        {
            // Tokens of deactivated or deleted accounts stop working immediately.
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(JwtTokenGenerator.UserIdClaimType)?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var isActive = Guid.TryParse(userId, out var id)
                               && await db.Users.AnyAsync(u => u.Id == id && u.IsActive, context.HttpContext.RequestAborted);
                if (!isActive)
                    context.Fail("The account is inactive.");
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(ApiResponse.Fail("Authentication is required. Please sign in."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(ApiResponse.Fail("You do not have permission to perform this action."));
            }
        };
    });
builder.Services.AddAuthorization(Policies.Register);
builder.Services.AddRateLimiter(RateLimitPolicies.Register);

// ----- CORS -----
const string corsPolicy = "Frontend";
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy(corsPolicy, p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition")));

// ----- Swagger -----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Exam Platform API",
        Version = "v1",
        Description = "Exam management platform for university teachers."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by POST /api/auth/login."
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
    var xml = Path.Combine(AppContext.BaseDirectory, "ExamPlatform.Api.xml");
    if (File.Exists(xml))
        c.IncludeXmlComments(xml);
});

var app = builder.Build();

// ----- Database initialization (Development) -----
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync(
        applyMigrations: configuration.GetValue("Database:ApplyMigrationsOnStartup", false));
}

// ----- Pipeline -----
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "Exam Platform API");
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous().ExcludeFromDescription();

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
