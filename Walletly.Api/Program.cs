using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Walletly.Application.Common;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.Interfaces;
using Walletly.Api.Authorization;
using Walletly.Api.Responses;
using Walletly.Application.Services;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.BackgroundJobs;
using Walletly.Infrastructure.Data;
using Walletly.Infrastructure.Notifications;
using Walletly.Infrastructure.Repositories.Postgres;
using Walletly.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Console plus a plain-text file under <project>/logs/walletly-api.log, so it can be tailed.
// The file name stays fixed (no date in it) and the folder and file are created on first write.
// Anchored to ContentRoot so the location does not depend on the working directory.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "logs", "walletly-api.log"),
        rollingInterval: RollingInterval.Infinite,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        retainedFileCountLimit: 10));

// Add services to the container.
builder.Services.AddControllers();

// Model-binding and [ApiController] validation failures use the same envelope as every other error.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError
            {
                Code = "Validation.Invalid",
                Message = string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The value is not valid." : error.ErrorMessage,
                // JSON parse errors use a JSON path such as "$.email". Strip it so the field matches the request body.
                Field = JsonNamingPolicy.CamelCase.ConvertName(entry.Key.StartsWith("$.", StringComparison.Ordinal) ? entry.Key[2..] : entry.Key)
            }))
            .ToList();

        return new BadRequestObjectResult(ApiResponse.Failure("One or more fields are invalid.", errors));
    };
});

// Configure Database Connection (from User Secrets or Environment)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<WalletlyDbContext>(options =>
    options.UseNpgsql(connectionString));

// Background jobs (Hangfire, PostgreSQL storage) and email notifications (Brevo)
builder.Services.AddBackgroundJobs(connectionString);
builder.Services.AddEmailNotifications(builder.Configuration);

// Configure JWT Settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// Register Core Authentication & RBAC Dependencies
builder.Services.AddScoped<IPlatformUserRepository, PostgresPlatformUserRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<WalletlyDbContext>());
builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPlatformAuthService, PlatformAuthService>();
builder.Services.AddScoped<PlatformDataSeeder>();

// Configure tenant webhook secret encryption
var webhookSecretSettings = new WebhookSecretSettings();
builder.Configuration.GetSection(WebhookSecretSettings.SectionName).Bind(webhookSecretSettings);

// Built eagerly so a missing or malformed key stops startup instead of failing on the first tenant request.
builder.Services.AddSingleton<IWebhookSecretProtector>(new AesWebhookSecretProtector(webhookSecretSettings));

// Tenant Auth & Settings
builder.Services.AddScoped<ITenantRepository, PostgresTenantRepository>();
builder.Services.AddScoped<ITenantUserRepository, PostgresTenantUserRepository>();
builder.Services.AddScoped<ITenantAccountOwnerRepository, PostgresTenantAccountOwnerRepository>();
builder.Services.AddScoped<ITenantRoleRepository, PostgresTenantRoleRepository>();
builder.Services.AddScoped<ITemporaryTenantRepository, PostgresTemporaryTenantRepository>();
builder.Services.AddScoped<ITenantTokenGenerator, TenantJwtTokenGenerator>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantAuthService, TenantAuthService>();
builder.Services.AddScoped<ITenantUserService, TenantUserService>();
builder.Services.AddScoped<ITenantRoleService, TenantRoleService>();

// Tenant onboarding: waitlist, admin invitations, self-service OTP, and activation
var onboardingOptions = new OnboardingOptions();
builder.Configuration.GetSection(OnboardingOptions.SectionName).Bind(onboardingOptions);
builder.Services.AddSingleton(onboardingOptions);
builder.Services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };

    // Without these, the 401 and 403 from the JWT middleware have an empty body instead of the envelope.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse.Failure(
                "Authentication is required.",
                [new ApiError { Code = "Auth.Unauthenticated", Message = "Authentication is required." }]));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse.Failure(
                "You do not have permission to do this.",
                [new ApiError { Code = "Auth.Forbidden", Message = "You do not have permission to do this." }]));
        }
    };
})
.AddScheme<AuthenticationSchemeOptions, Walletly.Api.Authentication.ApiKeyAuthenticationHandler>(
    Walletly.Api.Authentication.ApiKeyDefaults.AuthenticationScheme, null);

builder.Services.AddAuthorization(options =>
{
    // Platform and tenant tokens share one signing key. user_type keeps each token inside its own surface.
    options.AddPolicy(AuthorizationPolicies.PlatformTenantsWrite, policy => policy
        .RequireClaim("user_type", "platform_user")
        .RequireClaim("permission", "tenants.write"));

    options.AddPolicy(AuthorizationPolicies.TenantUser, policy => policy
        .RequireClaim("user_type", "tenant_user")
        .RequireClaim("tenant_id"));

    options.AddPolicy(AuthorizationPolicies.TenantApiKey, policy => policy
        .AddAuthenticationSchemes(Walletly.Api.Authentication.ApiKeyDefaults.AuthenticationScheme)
        .RequireClaim("tenant_id")
        .RequireClaim("environment_id"));
});

// OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Walletly API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Auto-apply EF Core Migrations and seed initial platform admin at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WalletlyDbContext>();
    try
    {
        db.Database.Migrate();

        var seeder = scope.ServiceProvider.GetRequiredService<PlatformDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations or seeding data.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Unhandled exceptions return the same envelope. The details are logged, not sent to the client.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is not null)
    {
        app.Logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(ApiResponse.Failure(
        "An unexpected error occurred. Please try again.",
        [new ApiError { Code = "Server.Unexpected", Message = "An unexpected error occurred." }]));
}));

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
