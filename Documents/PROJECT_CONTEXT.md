# API Ticket Application - Middleware Architecture & Testing Documentation

## Project Overview
- **Project Name**: API_TICKET_APPLICATION
- **.NET Version**: .NET 10
- **C# Version**: 14.0
- **IDE**: Visual Studio Community 2026
- **Framework**: ASP.NET Core
- **Kestrel Configuration**: 
  - HTTP listener: localhost:5924
  - HTTPS listener: localhost:5925 (with certificate)
- **HTTPS Redirect**: Configured via AddHttpsRedirection with HttpsPort = 5925

---

## Middleware Order & Purpose

Middleware chạy theo thứ tự từ trên xuống (pipeline):

### 1. Exception Handling (đặt đầu tiên)
- **Dev**: UseDeveloperExceptionPage + MapOpenApi
- **Prod**: UseExceptionHandler("/Error") + UseHsts
- **Mục đích**: Xử lý ngoại lệ toàn cục

### 2. Circuit Breaker
- **Vị trí**: Đầu ống (Early)
- **Cơ chế**: Buffer response stream, kiểm tra StatusCode >= 500
- **Hành động**: Thay 500+ bằng 503 ServiceUnavailable + message "Server quá tải, vui lòng thử lại sau."
- **Chi tiết**: Sử dụng MemoryStream để buffer, sau đó inspect và modify response body
- **Lưu ý**: Tránh buffer lớn cho streaming/file responses

### 3. Input Validation
- **Vị trí**: Sau circuit-breaker
- **Kiểm tra**: Giá trị query đã decode (Request.Query collection)
- **Patterns nguy hiểm**: <script>, DROP TABLE, UNION SELECT, --, ;--, /*, */, @@, char(, nchar(, varchar(, alter, exec, xp_
- **Hành động**: Trả 400 BadRequest + message "Dữ liệu đầu vào không hợp lệ."
- **Chi tiết**: Kiểm tra tất cả query values, case-insensitive match

### 4. Secure Query Middleware
- **Vị trí**: Sau input validation
- **Logic**: Chỉ enforce khi query parameter "secure" **tồn tại**
- **Hành động**: 
  - Nếu "secure" không tồn tại → cho phép (pass to next)
  - Nếu "secure" = "true" → cho phép
  - Nếu "secure" = khác "true" (ví dụ "false") → trả 400 BadRequest
- **Message**: "Truy vấn chứa dữ liệu nguy hiểm."
- **Chi tiết**: StringComparison.OrdinalIgnoreCase

### 5. Delay Request (Asynchronous Simulator)
- **Vị trí**: Sau secure query
- **Mục đích**: Giả lập độ trễ xử lý
- **Cố định**: 200ms (Task.Delay(200))
- **Log**: Console.WriteLine với path request

### 6. Security Log Events
- **Vị trí**: Tương đối sớm (pre-routing)
- **Ghi nhận**: 
  - Incoming: Method + Path
  - Outgoing: StatusCode
- **Mục đích**: Audit/monitoring request lifecycle

### 7. Logging / Elapsed Time
- **Vị trí**: Tương đối sớm
- **Đo lường**: Thời gian xử lý bằng Stopwatch (ms)
- **Log**: Console.WriteLine với elapsed time

### 8. UseHttpsRedirection
- **Cấu hình**: AddHttpsRedirection(options => options.HttpsPort = 5925)
- **Mục đích**: Redirect HTTP → HTTPS
- **Behavior**: HTTP (5924) → HTTPS (5925)
- **Lưu ý**: Client phải follow 301/307 redirect; một số HTTP clients không tự động follow

### 9. UseRouting
- **Vị trí**: Sau security middleware
- **Mục đích**: Route matching (Endpoint selection)

### 10. UseAuthorization
- **Vị trí**: Sau routing
- **Mục đích**: Authorization (chưa implement trong project này)

### 11. MapControllers / MapGet
- **Vị trí**: Cuối (Endpoint mapping)
- **Endpoints**:
  - GET / → "Hello World!" (MapGet)
  - GET /api/test → TestController.Get(...) (MapControllers)

---

## Endpoints

### 1. GET /
- **Handler**: app.MapGet("/", () => "Hello World!")
- **Response**: "Hello World!" (plain text)
- **Used for**: Testing root endpoint + HTTPS redirect

### 2. GET /api/test
- **Handler**: TestController.Get(...)
- **Query Parameters**:
  - `load` (string, optional): "heavy" → return 500 (trigger circuit-breaker)
  - `query` (string, optional): XSS/SQLi payload test
  - `secure` (string, optional): "true"/"false" (test secure-middleware)
  - `delay` (int, optional): milliseconds to wait (test delay middleware)
- **Response** (200 OK):
  ```json
  {
    "path": "/api/test",
    "queryString": "?...",
    "load": null,
    "query": "...",
    "secure": null,
    "delay": null,
    "message": "Processed"
  }
  ```
- **Special Case** (load=heavy):
  - Upstream status: 500
  - Circuit-breaker replaces: 503 + message

---

## Test Cases (Controllers/request.http)

### Test 1: Hello World HTTP → HTTPS Redirect
```
GET http://localhost:5924/
```
- **Expected**: 301/307 redirect to https://localhost:5925/
- **Purpose**: Verify HTTPS redirection middleware

### Test 2: Hello World HTTPS
```
GET https://localhost:5925/
```
- **Expected**: 200 OK, body = "Hello World!"
- **Purpose**: Verify endpoint works over HTTPS

### Test 3: Circuit Breaker (Heavy Load)
```
GET https://localhost:5925/api/test?load=heavy
```
- **Expected**: 503 ServiceUnavailable, body = "Server quá tải, vui lòng thử lại sau."
- **Purpose**: Verify circuit-breaker replaces 500+ with 503
- **Middleware Chain**: Request → Controller (500) → Circuit-breaker (503) → Response

### Test 4: Input Validation (XSS - URL-Encoded)
```
GET http://localhost:5924/api/test?query=%3Cscript%3Ealert(%27xss%27)%3C%2Fscript%3E
```
- **Decoded Value**: <script>alert('xss')</script>
- **Expected**: 400 BadRequest, body = "Dữ liệu đầu vào không hợp lệ."
- **Purpose**: Verify XSS input validation (middleware checks decoded values)
- **Attack Type**: Reflected XSS

### Test 5: Input Validation (SQLi)
```
GET http://localhost:5924/api/test?query=DROP%20TABLE%20Users
```
- **Decoded Value**: DROP TABLE Users
- **Expected**: 400 BadRequest
- **Purpose**: Verify SQL injection patterns are blocked
- **Attack Type**: SQL Injection

### Test 6: Secure Query (No Flag - Allowed)
```
GET http://localhost:5924/api/test
```
- **Expected**: 200 OK (after HTTPS redirect)
- **Purpose**: Verify no "secure" param = allowed
- **Middleware Behavior**: Secure middleware passes when param missing

### Test 7: Secure Query (secure=false - Blocked)
```
GET http://localhost:5924/api/test?secure=false
```
- **Expected**: 400 BadRequest, body = "Truy vấn chứa dữ liệu nguy hiểm."
- **Purpose**: Verify secure-middleware blocks when flag ≠ "true"

### Test 8: Secure Query (secure=true - Allowed)
```
GET https://localhost:5925/api/test?secure=true
```
- **Expected**: 200 OK
- **Purpose**: Verify secure=true is allowed

### Test 9: Delay Request (Simulated Processing)
```
GET https://localhost:5925/api/test?delay=2000
```
- **Expected**: 200 OK (after ~2000ms + 200ms middleware delay = ~2200ms total)
- **Purpose**: Test delay middleware + endpoint delay parameter
- **Timing**: Middleware adds 200ms base delay, endpoint adds 2000ms

---

## Issues Fixed During Development

### Issue 1: Secure Query Middleware blocking /
- **Problem**: Middleware was blocking all requests without checking if "secure" param exists
- **Solution**: Added ContainsKey check — only enforce when param exists
- **Code Change**:
  ```csharp
  if (context.Request.Query.ContainsKey("secure"))
  {
      // Only then check the value
  }
  ```

### Issue 2: HTTPS Redirect not working
- **Problem**: app.UseHttpsRedirection(options) — no overload accepts options directly
- **Solution**: Use AddHttpsRedirection in services + parameterless UseHttpsRedirection
- **Code Change**:
  ```csharp
  builder.Services.AddHttpsRedirection(options =>
  {
      options.HttpsPort = 5925;
  });
  app.UseHttpsRedirection(); // no params
  ```

### Issue 3: Circuit-breaker middleware not intercepting 500s from controller
- **Problem**: controller.Get() returns StatusCode(500, "…"), response body already written → middleware can't modify
- **Solution**: Buffer response stream before next() to intercept and modify
- **Code Change**:
  ```csharp
  var originalBody = context.Response.Body;
  using var buffer = new MemoryStream();
  context.Response.Body = buffer;

  await next();

  // Now inspect/modify before writing to originalBody
  if (context.Response.StatusCode >= 500) { ... }
  ```

### Issue 4: Input validation not catching URL-encoded payloads
- **Problem**: Middleware checked raw QueryString (?query=%3C…) instead of decoded values
- **Solution**: Use context.Request.Query (auto-decoded) instead of QueryString.Value
- **Code Change**:
  ```csharp
  // Before: context.Request.QueryString.Value (encoded)
  // After:
  var input = string.Join(" ", context.Request.Query.SelectMany(kv => kv.Value));
  ```

---

## Console Output Example

When running a request like GET /api/test?query=<script>…:

```
[SECURITY LOG] Incoming request: GET /api/test
[DELAY] Request /api/test bị trì hoãn 0.2 giây
Request /api/test took 205 ms
[SECURITY LOG] Response status: 400
```

---

## Security Considerations

### Threats Mitigated
1. **XSS (Cross-Site Scripting)**: Input validation blocks <script>, javascript patterns
2. **SQL Injection**: Patterns like DROP TABLE, UNION SELECT blocked
3. **HTTPS**: Enforced redirect from HTTP → HTTPS
4. **Server Overload**: Circuit-breaker returns 503 for 5xx errors
5. **Dangerous Query Flags**: Secure middleware prevents misuse

### Gaps / Future Improvements
- No CSRF protection (add AntiForgery middleware if forms used)
- No rate limiting (implement IRateLimitService)
- No detailed logging to file (add Serilog)
- XSS validation is pattern-based (use HtmlSanitizer for HTML content)
- No CORS policy configured
- No authentication/authorization implemented

---

## Running the Application

### Prerequisites
- .NET 10 SDK
- Development certificate (for HTTPS): `dotnet dev-certs https --trust`

### Steps
1. Open project in Visual Studio 2026
2. Set TestController.cs file or project as Startup
3. Press F5 (Debug) or Ctrl+F5 (Run)
4. Console output shows: "Kestrel đang lắng nghe trên http://localhost:5924"
5. Open Controllers/request.http and execute requests

### Testing Endpoints
- **Via REST client** (HTTP file, Postman, curl):
  ```powershell
  curl -v http://localhost:5924/
  curl -v https://localhost:5925/api/test?load=heavy
  ```
- **Via Browser**: https://localhost:5925/ → "Hello World!"

---

## File Structure
```
F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\
├── Program.cs                          (middleware pipeline + Kestrel config)
├── Controllers/
│   ├── TestController.cs               (endpoint: GET /api/test)
│   └── request.http                    (test cases)
├── Properties/
│   └── launchSettings.json             (profile config)
└── [other project files]
```

---

## Quick Reference: Middleware Order
```
1. Exception Handling
2. Circuit-breaker (buffer + 503 on 5xx)
3. Input Validation (decode check)
4. Secure Query (param validation)
5. Delay (200ms sim)
6. Security Log (audit)
7. Logging / Elapsed Time
8. HTTPS Redirect (HTTP → HTTPS)
9. Routing
10. Authorization
11. Endpoints (MapControllers + MapGet)
```

---

**Last Updated**: Current session  
**Status**: All middleware functional, ready for integration testing
