using System.Text;
using System.Threading.RateLimiting;
using Khel_Akhel_Server.BL.Common;
using Khel_Akhel_Server.BL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using static Khel_Akhel_Server.Models.CommonResponse;

var builder = WebApplication.CreateBuilder(args);

// CLI Command: dotnet run -- set-password --email <email>
if (args.Length > 0 && string.Equals(args[0], "set-password", StringComparison.OrdinalIgnoreCase))
{
    int exitCode = await SetPasswordCli.RunAsync(args, builder.Configuration);
    Environment.Exit(exitCode);
    return;
}

// Add services to the container.
builder
    .Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context
                .ModelState.Values.SelectMany(v => v.Errors)
                .Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? e.Exception?.Message ?? "Validation error"
                        : e.ErrorMessage
                )
                .ToList();

            bool isPayloadTooLarge = errors.Any(e =>
                e.Contains("limit", StringComparison.OrdinalIgnoreCase)
                || e.Contains("large", StringComparison.OrdinalIgnoreCase)
                || e.Contains("length", StringComparison.OrdinalIgnoreCase)
            );

            int statusCode = isPayloadTooLarge
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
            string message = isPayloadTooLarge ? "PAYLOAD_TOO_LARGE" : "VALIDATION_FAILED";

            var response = new ApiResponse
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors,
                Data = null,
            };

            return new ObjectResult(response) { StatusCode = statusCode };
        };
    });
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

// Configure Forwarded Headers for reverse proxies (IIS/nginx)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var knownProxiesConfig = builder
        .Configuration.GetSection("Security:KnownProxies")
        .Get<string[]>();
    if (knownProxiesConfig != null && knownProxiesConfig.Length > 0)
    {
        foreach (var proxy in knownProxiesConfig)
        {
            if (System.Net.IPAddress.TryParse(proxy, out var ip))
            {
                options.KnownProxies.Add(ip);
            }
        }
    }
});

// Configure Partitioned Rate Limiting per Client IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    int globalLimit = builder.Configuration.GetValue<int>("RateLimiting:GlobalLimit", 300);
    int globalWindow = builder.Configuration.GetValue<int>("RateLimiting:GlobalWindowMinutes", 1);
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = globalLimit,
                Window = TimeSpan.FromMinutes(globalWindow),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }
        );
    });

    int authLoginLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthLoginLimit", 10);
    int authLoginWindow = builder.Configuration.GetValue<int>(
        "RateLimiting:AuthLoginWindowMinutes",
        1
    );
    options.AddPolicy(
        "auth-login",
        httpContext =>
        {
            string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authLoginLimit,
                    Window = TimeSpan.FromMinutes(authLoginWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            );
        }
    );

    int authSessionLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthSessionLimit", 60);
    int authSessionWindow = builder.Configuration.GetValue<int>(
        "RateLimiting:AuthSessionWindowMinutes",
        1
    );
    options.AddPolicy(
        "auth-session",
        httpContext =>
        {
            string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authSessionLimit,
                    Window = TimeSpan.FromMinutes(authSessionWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            );
        }
    );

    int publicFormLimit = builder.Configuration.GetValue<int>("RateLimiting:PublicFormLimit", 5);
    int publicFormWindow = builder.Configuration.GetValue<int>(
        "RateLimiting:PublicFormWindowMinutes",
        10
    );
    options.AddPolicy(
        "public-form",
        httpContext =>
        {
            string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = publicFormLimit,
                    Window = TimeSpan.FromMinutes(publicFormWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            );
        }
    );

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = (
                (int)Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds))
            ).ToString();
        }

        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var policy =
            context
                .HttpContext.GetEndpoint()
                ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
                ?.PolicyName
            ?? "GlobalLimiter";
        Logs.Warning($"Rate limit exceeded | Policy: {policy} | IP: {ip}");

        var response = new ApiResponse
        {
            Success = false,
            StatusCode = StatusCodes.Status429TooManyRequests,
            Message = "RATE_LIMIT_EXCEEDED",
            Errors = new List<string> { "Rate limit exceeded. Please try again later." },
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});

// Register Application Infrastructure Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<DbHelper>();
builder.Services.AddScoped<IUrlEncryptionService, UrlEncryptionService>();
builder.Services.AddScoped<IUrlSignatureService, UrlSignatureService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

// Fail-fast security secret validation at startup
StartupSecurityValidator.Validate(builder.Configuration);

// Configure JWT Authentication & Options
string jwtKey = builder.Configuration["Jwt:Key"]!;
var key = Encoding.UTF8.GetBytes(jwtKey);
string jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
string jwtAudience = builder.Configuration["Jwt:Audience"]!;

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

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
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

app.UseForwardedHeaders();

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

// Handle BadHttpRequestException (e.g. 413 Payload Too Large from file size limits) in standard ApiResponse shape
app.Use(
    async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (BadHttpRequestException ex)
        {
            int statusCode =
                ex.StatusCode == StatusCodes.Status413PayloadTooLarge
                || ex.Message.Contains("large", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status413PayloadTooLarge
                    : (ex.StatusCode > 0 ? ex.StatusCode : StatusCodes.Status400BadRequest);

            string messageCode =
                statusCode == StatusCodes.Status413PayloadTooLarge
                    ? "PAYLOAD_TOO_LARGE"
                    : "BAD_REQUEST";

            Logs.Warning(
                $"BadHttpRequestException handled from IP {context.Connection.RemoteIpAddress}: {ex.Message}"
            );
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                var response = new ApiResponse
                {
                    Success = false,
                    StatusCode = statusCode,
                    Message = messageCode,
                    Errors = new List<string> { ex.Message },
                    Data = null,
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
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

public partial class Program { }
