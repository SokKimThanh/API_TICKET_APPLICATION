using Microsoft.AspNetCore.HttpsPolicy;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;

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
{
    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
app.UseAuthorization();

// 6. Endpoints
app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.Run();


public static class InputValidator
{
    // Hàm kiểm tra dữ liệu đầu vào
    public static bool IsInvalid(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Các pattern nguy hiểm cần lọc
        string[] dangerousPatterns = new[]
        {
            "<script>", "DROP TABLE", "UNION SELECT", "--", ";--", "/*", "*/", "@@", "char(", "nchar(", "varchar(", "alter", "exec", "xp_"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
