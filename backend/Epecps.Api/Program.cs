using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

builder.Logging.AddConsole();

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
    .AddMicrosoftIdentityWebApi(
        jwtOptions =>
        {
            // default scheme; nothing special
        },
        identityOptions =>
        {
            identityOptions.Instance = config["AzureAd:Instance"] ?? "";
            identityOptions.TenantId = config["AzureAd:TenantId"] ?? "";
            identityOptions.ClientId = config["AzureAd:ClientId"] ?? "";   // the API app's client id
            // Note: Audience validation is handled automatically by Microsoft.Identity.Web
            // using the ClientId. If you need custom audience validation, configure it via TokenValidationParameters
        },
        // Configuration section name (optional if you set above)
        "AzureAd");

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

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SpaDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
