using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.Interfaces;
using Walletly.Api.Authorization;
using Walletly.Application.Services;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.BackgroundJobs;
using Walletly.Infrastructure.Data;
using Walletly.Infrastructure.Notifications;
using Walletly.Infrastructure.Repositories;
using Walletly.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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
builder.Services.AddScoped<ITenantTokenGenerator, TenantJwtTokenGenerator>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantAuthService, TenantAuthService>();

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
});

builder.Services.AddAuthorization(options =>
{
    // Platform and tenant tokens share one signing key. user_type keeps each token inside its own surface.
    options.AddPolicy(AuthorizationPolicies.PlatformTenantsWrite, policy => policy
        .RequireClaim("user_type", "platform_user")
        .RequireClaim("permission", "tenants.write"));

    options.AddPolicy(AuthorizationPolicies.TenantUser, policy => policy
        .RequireClaim("user_type", "tenant_user")
        .RequireClaim("tenant_id"));
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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
