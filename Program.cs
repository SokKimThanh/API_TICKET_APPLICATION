using Microsoft.AspNetCore.HttpsPolicy;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
        {
            Modifiers = { JsonContractModifiers.IgnoreVirtualPropertiesModifier }
        };
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
    {
        Modifiers = { JsonContractModifiers.IgnoreVirtualPropertiesModifier }
    };
});

builder.Services.AddOpenApi(options =>
{
    // Custom Schema ID generation for nested generic classes to avoid raw names (like ResponseModel_1)
    options.CreateSchemaReferenceId = jsonType =>
    {
        var defaultId = Microsoft.AspNetCore.OpenApi.OpenApiOptions.CreateDefaultSchemaReferenceId(jsonType);
        if (defaultId is null)
        {
            return null;
        }

        return GetFriendlySchemaId(jsonType.Type);
    };

    static string GetFriendlySchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name;
            var index = name.IndexOf('`');
            if (index > 0)
            {
                name = name.Substring(0, index);
            }
            var genericArgs = type.GetGenericArguments();
            var genericArgsFriendly = string.Join("And", genericArgs.Select(GetFriendlySchemaId));
            return $"{name}Of{genericArgsFriendly}";
        }
        return type.Name;
    }

    // Configure security schemes in OpenAPI Document (for JWT "Authorize" unlock button in Swagger UI)
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
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
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API Ticket Application v1");
        options.RoutePrefix = "swagger"; // Enables access at /swagger
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

public sealed class BearerSecuritySchemeTransformer : Microsoft.AspNetCore.OpenApi.IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        Microsoft.OpenApi.OpenApiDocument document,
        Microsoft.AspNetCore.OpenApi.OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Hãy nhập JWT Token của bạn để xác thực các endpoint bảo mật (không cần tiền tố Bearer)."
        };

        document.Security ??= new List<Microsoft.OpenApi.OpenApiSecurityRequirement>();

        var requirement = new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        };
        document.Security.Add(requirement);

        return Task.CompletedTask;
    }
}

public static class JsonContractModifiers
{
    public static void IgnoreVirtualPropertiesModifier(System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object)
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
