using System.Text;
using System.Data;
using Epecps.Application.Interfaces;
using Epecps.Application.Models;
using Epecps.Infrastructure.Persistence;
using Epecps.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;

services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
services.Configure<JwtSettings>(config.GetSection("Jwt"));
services.Configure<SuperAdminSettings>(config.GetSection("SuperAdmin"));

var ignorePendingModelChangesWarning = config.GetValue("Database:IgnorePendingModelChangesWarning", false);

services.AddDbContext<EpecpsDbContext>(options =>
{
    options.UseSqlServer(config.GetConnectionString("DefaultConnection"));

    if (ignorePendingModelChangesWarning)
    {
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
});

services.AddScoped<Epecps.Infrastructure.Data.DatabaseSeeder>();

services.AddScoped<IEmailService, EmailService>();
services.AddHostedService<EmailBackgroundService>();

services.AddScoped<IReportService, ReportService>();

services.AddScoped<IScoreTemplateService, ScoreTemplateService>();
services.AddScoped<IScoreCategoryService, ScoreCategoryService>();
services.AddScoped<IScoreItemService, ScoreItemService>();

services.AddScoped<IGoalFrameworkService, GoalFrameworkService>();
services.AddScoped<IPersonalGoalService, PersonalGoalService>();
services.AddScoped<IUserSyncService, UserSyncService>();
services.AddScoped<IRmGoalAssignmentService, RmGoalAssignmentService>();

services.AddScoped<IEvaluationWorkflowService, EvaluationWorkflowService>();
services.AddScoped<IReviewScoringService, ReviewScoringService>();

services.AddScoped<IDashboardService, DashboardService>();

services.AddScoped<IPasswordService, PasswordService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IUserProjectImportService, UserProjectImportService>();
services.AddScoped<ISuperAdminBootstrapService, SuperAdminBootstrapService>();
services.AddScoped<IWorkflowV2Service, WorkflowV2Service>();

var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins =
    [
        "http://127.0.0.1:64291",
        "http://localhost:64291",
        "http://127.0.0.1:4200",
        "http://localhost:4200"
    ];
}

services.AddCors(opt =>
{
    opt.AddPolicy("SpaDev", p =>
        p.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var jwtSection = config.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
{
    throw new InvalidOperationException("Jwt:SigningKey is required.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey));

services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", p => p.RequireRole("SuperAdmin"));
    options.AddPolicy("RequireAdminOrSuperAdmin", p => p.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("RequireRM", p => p.RequireRole("RM", "SuperAdmin"));
    options.AddPolicy("RequireHOD", p => p.RequireRole("HOD", "SuperAdmin"));
});

services.AddControllers();
services.AddEndpointsApiExplorer();

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Epecps API",
        Version = "v1",
        Description = "Employee Performance Evaluation and Career Progression System API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var autoMigrate = config.GetValue("Database:AutoMigrate", false);
var autoSeed = config.GetValue("Database:AutoSeed", false);
var migrateOnly = config.GetValue("Database:MigrateOnly", false);
var recreateIfCoreTablesMissing = config.GetValue("Database:RecreateIfCoreTablesMissing", false);
var startupRetryCount = Math.Max(1, config.GetValue("Database:StartupRetryCount", 1));
var startupRetryDelaySeconds = Math.Max(1, config.GetValue("Database:StartupRetryDelaySeconds", 5));
var disableHttpsRedirection = config.GetValue("DisableHttpsRedirection", false);

await InitializeDatabaseAsync(
    app,
    autoMigrate,
    autoSeed,
    ignorePendingModelChangesWarning,
    recreateIfCoreTablesMissing,
    startupRetryCount,
    TimeSpan.FromSeconds(startupRetryDelaySeconds));

try
{
    await EnsureSuperAdminAsync(app);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Super admin bootstrap skipped due to current schema state.");
}

if (migrateOnly)
{
    app.Logger.LogInformation("Database migrate-only mode complete. Exiting process.");
    return;
}

async Task EnsureSuperAdminAsync(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var bootstrap = scope.ServiceProvider.GetRequiredService<ISuperAdminBootstrapService>();
    await bootstrap.EnsureSuperAdminAsync();
}

async Task InitializeDatabaseAsync(
    WebApplication webApp,
    bool runMigrations,
    bool runSeeder,
    bool ignorePendingModelChanges,
    bool recreateIfCoreTablesMissing,
    int retryCount,
    TimeSpan retryDelay)
{
    for (var attempt = 1; attempt <= retryCount; attempt++)
    {
        try
        {
            using var scope = webApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EpecpsDbContext>();

            if (runMigrations)
            {
                try
                {
                    await db.Database.MigrateAsync();
                }
                catch (InvalidOperationException ex) when (
                    ignorePendingModelChanges &&
                    ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase))
                {
                    webApp.Logger.LogWarning(
                        ex,
                        "EF migration validation reported pending model changes; continuing with idempotent schema sync.");
                }
                catch (Exception ex) when (ignorePendingModelChanges)
                {
                    webApp.Logger.LogWarning(
                        ex,
                        "Database migration failed in relaxed mode; continuing with fallback schema sync.");
                }
            }

            var usersTableExists = await TableExistsAsync(db, "Users");
            if (!usersTableExists)
            {
                webApp.Logger.LogWarning(
                    "Users table was not found after migration attempt. Running EnsureCreated fallback.");
                await db.Database.EnsureCreatedAsync();
            }

            if (recreateIfCoreTablesMissing)
            {
                var requiredTables = new[] { "Users", "Roles", "Departments" };
                var missingTables = new List<string>();

                foreach (var table in requiredTables)
                {
                    if (!await TableExistsAsync(db, table))
                    {
                        missingTables.Add(table);
                    }
                }

                if (missingTables.Count > 0)
                {
                    webApp.Logger.LogWarning(
                        "Detected partial schema (missing: {MissingTables}). Recreating database with EnsureCreated.",
                        string.Join(", ", missingTables));

                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                }
            }

            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
        IF OBJECT_ID(N'[Users]', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH('Users', 'PasswordHash') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PasswordHash] nvarchar(500) NULL;
            END

            IF COL_LENGTH('Users', 'PasswordSetAt') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [PasswordSetAt] datetime2 NULL;
            END

            IF COL_LENGTH('Users', 'LastLoginAt') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [LastLoginAt] datetime2 NULL;
            END

            IF COL_LENGTH('Users', 'LockedUntil') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [LockedUntil] datetime2 NULL;
            END

            IF COL_LENGTH('Users', 'FailedLoginCount') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [FailedLoginCount] int NOT NULL CONSTRAINT [DF_Users_FailedLoginCount] DEFAULT(0);
            END

            IF COL_LENGTH('Users', 'IsActive') IS NULL
            BEGIN
                ALTER TABLE [Users] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT(1);
            END

            IF OBJECT_ID(N'[Projects]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Projects] (
                    [ProjectId] int IDENTITY(1,1) NOT NULL,
                    [ProjectCode] nvarchar(100) NOT NULL,
                    [ProjectName] nvarchar(200) NOT NULL,
                    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_Projects_Status] DEFAULT(N'Active'),
                    [ProjectManagerUserId] int NULL,
                    [SupervisorUserId] int NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_Projects] PRIMARY KEY ([ProjectId]),
                    CONSTRAINT [FK_Projects_Users_ProjectManagerUserId] FOREIGN KEY ([ProjectManagerUserId]) REFERENCES [Users] ([UserId]),
                    CONSTRAINT [FK_Projects_Users_SupervisorUserId] FOREIGN KEY ([SupervisorUserId]) REFERENCES [Users] ([UserId])
                );
            END

            IF OBJECT_ID(N'[RefreshTokens]', N'U') IS NULL
            BEGIN
                CREATE TABLE [RefreshTokens] (
                    [RefreshTokenId] int IDENTITY(1,1) NOT NULL,
                    [UserId] int NOT NULL,
                    [TokenHash] nvarchar(500) NOT NULL,
                    [ExpiresAt] datetime2 NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [RevokedAt] datetime2 NULL,
                    [ReplacedByTokenHash] nvarchar(500) NULL,
                    [ReasonRevoked] nvarchar(500) NULL,
                    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([RefreshTokenId]),
                    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );
            END

            IF OBJECT_ID(N'[UserProjectAssignments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [UserProjectAssignments] (
                    [UserProjectAssignmentId] int IDENTITY(1,1) NOT NULL,
                    [UserId] int NOT NULL,
                    [ProjectId] int NOT NULL,
                    [AssignmentRole] nvarchar(100) NOT NULL,
                    [StartDate] datetime2 NULL,
                    [EndDate] datetime2 NULL,
                    [IsActive] bit NOT NULL CONSTRAINT [DF_UserProjectAssignments_IsActive] DEFAULT(1),
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    CONSTRAINT [PK_UserProjectAssignments] PRIMARY KEY ([UserProjectAssignmentId]),
                    CONSTRAINT [FK_UserProjectAssignments_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_UserProjectAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_Projects_ProjectCode' AND object_id = OBJECT_ID(N'[Projects]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_Projects_ProjectCode] ON [Projects] ([ProjectCode]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_Projects_ProjectManagerUserId' AND object_id = OBJECT_ID(N'[Projects]'))
            BEGIN
                CREATE INDEX [IX_Projects_ProjectManagerUserId] ON [Projects] ([ProjectManagerUserId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_Projects_SupervisorUserId' AND object_id = OBJECT_ID(N'[Projects]'))
            BEGIN
                CREATE INDEX [IX_Projects_SupervisorUserId] ON [Projects] ([SupervisorUserId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
            BEGIN
                CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_RefreshTokens_TokenHash' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_RefreshTokens_ExpiresAt' AND object_id = OBJECT_ID(N'[RefreshTokens]'))
            BEGIN
                CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_UserProjectAssignments_UserId' AND object_id = OBJECT_ID(N'[UserProjectAssignments]'))
            BEGIN
                CREATE INDEX [IX_UserProjectAssignments_UserId] ON [UserProjectAssignments] ([UserId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_UserProjectAssignments_ProjectId' AND object_id = OBJECT_ID(N'[UserProjectAssignments]'))
            BEGIN
                CREATE INDEX [IX_UserProjectAssignments_ProjectId] ON [UserProjectAssignments] ([ProjectId]);
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = 'IX_UserProjectAssignments_UserId_ProjectId' AND object_id = OBJECT_ID(N'[UserProjectAssignments]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_UserProjectAssignments_UserId_ProjectId] ON [UserProjectAssignments] ([UserId], [ProjectId]);
            END
        END

        IF OBJECT_ID(N'[Roles]', N'U') IS NOT NULL
        BEGIN
            EXEC(N'
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N''SuperAdmin'')
                BEGIN
                    INSERT INTO [Roles] ([Name]) VALUES (N''SuperAdmin'');
                END
            ');
        END
    ");
            }
            catch (Exception ex)
            {
                webApp.Logger.LogWarning(ex, "Idempotent schema sync skipped due to current database state.");
            }

            if (runSeeder)
            {
                try
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<Epecps.Infrastructure.Data.DatabaseSeeder>();
                    await seeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    webApp.Logger.LogWarning(ex, "Database seeding skipped due to partial schema state.");
                }
            }

            webApp.Logger.LogInformation("Database initialization completed.");
            return;
        }
        catch (Exception ex)
        {
            if (attempt == retryCount)
            {
                webApp.Logger.LogError(ex, "Database initialization failed after {AttemptCount} attempts.", retryCount);
                throw;
            }

            webApp.Logger.LogWarning(
                ex,
                "Database initialization attempt {Attempt}/{AttemptCount} failed. Retrying in {DelaySeconds} seconds...",
                attempt,
                retryCount,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay);
        }
    }
}

async Task<bool> TableExistsAsync(EpecpsDbContext dbContext, string tableName)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = false;

    if (connection.State != ConnectionState.Open)
    {
        await connection.OpenAsync();
        shouldClose = true;
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT CASE WHEN OBJECT_ID(@tableName, 'U') IS NULL THEN 0 ELSE 1 END";

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    var result = await command.ExecuteScalarAsync();
    if (shouldClose)
    {
        await connection.CloseAsync();
    }

    return result is not null && Convert.ToInt32(result) == 1;
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Epecps API v1");
    });
}

if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("SpaDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
