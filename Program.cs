using Microsoft.AspNetCore.HttpsPolicy;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5924, listen =>
    {
        Console.WriteLine("Kestrel đang lắng nghe trên http://localhost:5924");
    });

    options.ListenLocalhost(5925, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

// Register AppDbContext for Dependency Injection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== CONFIGURE JWT AUTHENTICATION ==========
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register OpenAPI services with Bearer authentication transformer and custom schema generator
builder.Services.AddOpenApi(options =>
{
    // Custom Schema ID Generator for Generic types in native .NET 10 OpenAPI
    options.AddSchemaTransformer((schema, context, cancellationToken) =>
    {
        var type = context.JsonTypeInfo.Type;
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().Name;
            if (genericTypeName.Contains('`'))
            {
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
            }
            var genericArgs = string.Join("And", type.GetGenericArguments().Select(t => t.Name));
            schema.Title = $"{genericTypeName}Of{genericArgs}";
        }
        else
        {
            schema.Title = type.Name;
        }
        return Task.CompletedTask;
    });

    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure custom modifiers to avoid circular reference loop issues with EF Core Navigation properties
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonContractModifiers.IgnoreVirtualPropertiesModifier }
        };
    });

// Configure the same modifier for Minimal API / OpenAPI pipeline
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { JsonContractModifiers.IgnoreVirtualPropertiesModifier }
    };
});

// Configure HTTPS redirection options so middleware knows the HTTPS port (Kestrel listens on 5925)
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5925;
});

var app = builder.Build();

// 1. Exception handling (đặt đầu tiên)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi(); // Generates JSON at /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
        options.RoutePrefix = "swagger"; // Exposes Swagger UI at /swagger
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // chỉ bật trong production
}

// circuit breaker
app.Use(async (context, next) =>
{
    await next();

    if(context.Response.StatusCode >= 500)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Server quá tải, vui lòng thử lại sau.");
        }
        return;
    }
});

// input validation
app.Use(async (context, next) =>
{
    // Collect decoded query values (Request.Query values are already URL-decoded)
    var input = string.Empty;
    if (context.Request.Query != null && context.Request.Query.Count > 0)
    {
        input = string.Join(" ", context.Request.Query.SelectMany(kv => kv.Value));
    }

    if (InputValidator.IsInvalid(input))
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Dữ liệu đầu vào không hợp lệ.");
        }
        return; // return som
    }
    await next();
});
 
// Secure Query Middleware
app.Use(async (context, next) =>
{
    // Only enforce when a 'secure' query parameter is present.
    // This avoids blocking requests that don't include any query (e.g. GET / ).
    if (context.Request.Query.ContainsKey("secure"))
    {
        var secureFlag = context.Request.Query["secure"].ToString();

        if (!string.Equals(secureFlag, "true", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Truy vấn chứa dữ liệu nguy hiểm.");
            }
            return; // return sớm
        }
    }

    await next.Invoke();
});

// Delay Request (Asynchronous Simulator)
app.Use(async (context, next) =>
{
    // Giả lập độ trễ 0.2 giây
    await Task.Delay(200);

    Console.WriteLine($"[DELAY] Request {context.Request.Path} bị trì hoãn 0.2 giây");

    await next.Invoke();
});

// 2. Security log events (đặt đầu ống)
app.Use(async (context, next) =>
{
    Console.WriteLine($"[SECURITY LOG] Incoming request: {context.Request.Method} {context.Request.Path}");

    await next.Invoke();

    // Sau khi các middleware khác chạy xong
    Console.WriteLine($"[SECURITY LOG] Response status: {context.Response.StatusCode}");
});

// 2.1 Logging / đo thời gian (Elapsed)
app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next.Invoke();
    sw.Stop();
    Console.WriteLine($"Request {context.Request.Path} took {sw.ElapsedMilliseconds} ms");
});

// 3. Security
// Use default HTTPS redirection middleware (options configured above)
app.UseHttpsRedirection();

// 4. Routing
app.UseRouting();

// 5. Auth
app.UseAuthentication();
app.UseAuthorization();

// 6. Endpoints
app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.Run();


public static class InputValidator
{
    // OPTIMIZATION: Moved patterns to a static readonly field to avoid allocation on every request.
    private static readonly string[] DangerousPatterns = new[]
    {
        "<script>", "DROP TABLE", "UNION SELECT", "--", ";--", "/*", "*/", "@@", "char(", "nchar(", "varchar(", "alter", "exec", "xp_"
    };

    // Hàm kiểm tra dữ liệu đầu vào
    public static bool IsInvalid(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

// ========== NATIVE OPENAPI SECURITY TRANSFORMER FOR .NET 10 ==========
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

    public BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    {
        _authenticationSchemeProvider = authenticationSchemeProvider;
    }

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
        {
            var requirements = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            // Correct instantiation of security requirements using the standard Microsoft.OpenApi classes
            var securityRequirement = new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            };
            document.Security ??= new List<OpenApiSecurityRequirement>();
            document.Security.Add(securityRequirement);
        }
    }
}

// ========== EF CORE CIRCULAR REFERENCE LOOP EXCLUDER ==========
public static class JsonContractModifiers
{
    public static void IgnoreVirtualPropertiesModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        // Ignore EF Core navigation/virtual properties to prevent circular references in JSON serialization and OpenAPI schemas.
        for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var prop = typeInfo.Properties[i];
            var propInfo = typeInfo.Type.GetProperty(prop.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (propInfo != null)
            {
                var isVirtual = (propInfo.GetMethod != null && propInfo.GetMethod.IsVirtual && !propInfo.GetMethod.IsFinal) ||
                                (propInfo.SetMethod != null && propInfo.SetMethod.IsVirtual && !propInfo.SetMethod.IsFinal);

                if (isVirtual)
                {
                    typeInfo.Properties.RemoveAt(i);
                }
            }
        }
    }
}
