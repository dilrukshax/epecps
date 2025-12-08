using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Epecps.Infrastructure.Persistence;
using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Epecps.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

// Email Settings Configuration
services.Configure<EmailSettings>(config.GetSection("EmailSettings"));

// Database Context
services.AddDbContext<EpecpsDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

// Database Seeder
services.AddScoped<Epecps.Infrastructure.Data.DatabaseSeeder>();

// Email Services
services.AddScoped<IEmailService, EmailService>();
services.AddHostedService<EmailBackgroundService>();

// Admin Framework Services
services.AddScoped<IScoreTemplateService, ScoreTemplateService>();
services.AddScoped<IScoreCategoryService, ScoreCategoryService>();
services.AddScoped<IScoreItemService, ScoreItemService>();

// Employee Goals Services
services.AddScoped<IGoalFrameworkService, GoalFrameworkService>();
services.AddScoped<IPersonalGoalService, PersonalGoalService>();
services.AddScoped<IUserSyncService, UserSyncService>(); // Auto-sync users from Azure AD

// Evaluation Workflow Services
services.AddScoped<IEvaluationWorkflowService, EvaluationWorkflowService>();

// FluentValidation
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<Epecps.Application.Validators.CreatePersonalGoalDtoValidator>();

// CORS for Angular dev on 64291
services.AddCors(opt =>
{
    opt.AddPolicy("SpaDev", p =>
        p.WithOrigins("http://127.0.0.1:64291", "http://localhost:64291")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// Microsoft Entra ID (Azure AD) protection for this API
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(config.GetSection("AzureAd"));

// Make "roles" work with [Authorize(Roles="Admin")]
services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = "roles";     // Entra v2 places app roles in "roles"
    options.TokenValidationParameters.NameClaimType = "name";
});

services.AddAuthorization(options =>
{
    // Policy to require the Epecps.ReadWrite scope (from OAuth)
    options.AddPolicy("RequireEpecpsScope", policy =>
        policy.Requirements.Add(new ScopeAuthorizationRequirement()
        {
            RequiredScopesConfigurationKey = "AzureAd:Scopes"
        }));

    // Optional: Role-based policies (require app roles created in API app registration)
    options.AddPolicy("RequireRM", p => p.RequireRole("RM"));
    options.AddPolicy("RequireHOD", p => p.RequireRole("HOD"));
    // add the rest as needed...
});

// ScopeAuthorizationHandler is registered automatically by AddMicrosoftIdentityWebApi
// No need to register it manually

services.AddControllers();
services.AddEndpointsApiExplorer();

// Configure Swagger with Azure AD OAuth2
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Epecps API",
        Version = "v1",
        Description = "Employee Performance Evaluation and Career Progression System API"
    });

    var tenantId = config["AzureAd:TenantId"];
    var clientId = config["AzureAd:ClientId"];
    var appIdUri = config["AzureAd:AppIdUri"];
    var instance = config["AzureAd:Instance"];

    // Define the OAuth2.0 scheme using Implicit flow (better compatibility with Swagger UI)
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{instance}{tenantId}/oauth2/v2.0/authorize"),
                Scopes = new Dictionary<string, string>
                {
                    { $"{appIdUri}/Epecps.ReadWrite", "Access the Epecps API" }
                }
            }
        },
        Description = "Azure AD OAuth2 Authentication"
    });

    // Make sure Swagger UI requires a Bearer token to be specified
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { $"{appIdUri}/Epecps.ReadWrite" }
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Epecps API v1");
        options.OAuthClientId(config["AzureAd:ClientId"]);
        options.OAuthScopes($"{config["AzureAd:AppIdUri"]}/Epecps.ReadWrite");
        options.OAuthScopeSeparator(" ");
    });
}

app.UseHttpsRedirection();
app.UseCors("SpaDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
