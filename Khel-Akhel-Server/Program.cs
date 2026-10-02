using System.Text;
using System.Threading.RateLimiting;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}",
        }
    );

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(
        "authentication",
        opt =>
        {
            opt.PermitLimit = 15;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 3;
        }
    );
    options.AddFixedWindowLimiter(
        "AuthPolicy",
        opt =>
        {
            opt.PermitLimit = 15;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 3;
        }
    );
    options.AddFixedWindowLimiter(
        "GeneralPolicy",
        opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        }
    );
});

// Register Application Infrastructure Services
builder.Services.AddScoped<DbHelper>();
builder.Services.AddScoped<IUrlEncryptionService, UrlEncryptionService>();
builder.Services.AddScoped<IUrlSignatureService, UrlSignatureService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// Configure JWT Authentication & Options
string jwtKey =
    builder.Configuration["Jwt:Key"] ?? "KhelAkhelSecretKeyForJWTAuthentication2026SecureKey!";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "KhelAkhelServer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "KhelAkhelClient",
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Email,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is not SecurityTokenExpiredException)
                {
                    Logs.Warning(
                        $"JWT authentication failed | Reason:{context.Exception.GetType().Name}"
                    );
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AngularPolicy",
        policy =>
        {
            var origins =
                builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200", "https://localhost:4200"];
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    );
});

var app = builder.Build();

// Application startup logging & log maintenance
Logs.CleanupOldLogs(45);
Logs.Info($"===== APPLICATION STARTED ===== Environment: {app.Environment.EnvironmentName}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// CORS MUST be invoked before HttpsRedirection & Authentication
app.UseCors("AngularPolicy");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Application stopping logging
app.Lifetime.ApplicationStopping.Register(() =>
{
    Logs.Info($"===== APPLICATION STOPPING ===== DateTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
});

app.Run();
