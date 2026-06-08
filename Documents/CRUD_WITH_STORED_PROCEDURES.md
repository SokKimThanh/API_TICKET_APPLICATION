# Movie CRUD with DbContext + Stored Procedures - Setup Guide

## Overview
MoviesController đã được cập nhật để:
- Sử dụng **AppDbContext** (Dependency Injection)
- Gọi **SQL Stored Procedures** từ database
- Xử lý error toàn cục (try-catch)
- Async/await pattern

---

## Step 1: Setup Database & Stored Procedures

### 1.1 Chạy SQL Script
1. Mở **SQL Server Management Studio (SSMS)**
2. Kết nối tới: `(localdb)\MSSQLLocalDB`
3. Chọn database: `TicketManagementDB`
4. Mở file: `SQL/movie-stored-procedures.sql`
5. Nhấn **Execute** (F5) để tạo 7 Stored Procedures:
   - `sp_GetAllMovies`
   - `sp_GetMovieById`
   - `sp_GetMovieByTitle`
   - `sp_CreateMovie`
   - `sp_UpdateMovie`
   - `sp_UpdateMoviePartial`
   - `sp_DeleteMovie`

### 1.2 Kiểm tra Stored Procedures
```sql
-- Xem danh sách SP trong SSMS
SELECT * FROM INFORMATION_SCHEMA.ROUTINES 
WHERE ROUTINE_NAME LIKE 'sp_Movie%' OR ROUTINE_NAME LIKE 'sp_%Movie%'
ORDER BY ROUTINE_NAME;
```

---

## Step 2: Program.cs Configuration

### 2.1 DbContext Dependency Injection
Đã thêm vào Program.cs:

```csharp
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;

// Register AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;");
});
```

**Lưu ý**: Connection string có thể được move sang `appsettings.json` cho security:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Rồi cập nhật Program.cs:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
```

---

## Step 3: MoviesController Endpoints

### Endpoint 1: GET /api/movies
```http
GET https://localhost:5925/api/movies
```

**Stored Procedure**: `sp_GetAllMovies`

**Response**:
```json
[
  {
    "id": 1,
    "title": "The Shawshank Redemption",
    "description": "Two imprisoned men...",
    "genre": "Drama",
    "durationInMinutes": 142,
    "posterUrl": "...",
    "releaseDate": "1994-09-23"
  }
]
```

---

### Endpoint 2: GET /api/movies/{id}
```http
GET https://localhost:5925/api/movies/1
```

**Stored Procedure**: `sp_GetMovieById @MovieId = 1`

**Response** (200):
```json
{
  "id": 1,
  "title": "The Shawshank Redemption",
  ...
}
```

**Response** (404):
```json
{
  "message": "Movie with ID 999 not found."
}
```

---

### Endpoint 3: POST /api/movies
```http
POST https://localhost:5925/api/movies
Content-Type: application/json

{
  "title": "Inception",
  "description": "A thief who steals corporate secrets...",
  "genre": "Sci-Fi",
  "durationInMinutes": 148,
  "posterUrl": "https://example.com/inception.jpg",
  "releaseDate": "2010-07-16"
}
```

**Stored Procedure**:
```sql
EXEC sp_CreateMovie 
  @Title = 'Inception',
  @Description = '...',
  @Genre = 'Sci-Fi',
  @DurationInMinutes = 148,
  @PosterUrl = '...',
  @ReleaseDate = '2010-07-16'
```

**Response** (201 Created):
```json
{
  "id": 3,
  "title": "Inception",
  ...
}
```

---

### Endpoint 4: PUT /api/movies/{id}
```http
PUT https://localhost:5925/api/movies/1
Content-Type: application/json

{
  "title": "Updated Title",
  "description": "Updated desc",
  "genre": "Drama",
  "durationInMinutes": 150,
  "posterUrl": "...",
  "releaseDate": "1994-09-23"
}
```

**Stored Procedure**:
```sql
EXEC sp_UpdateMovie
  @MovieId = 1,
  @Title = 'Updated Title',
  @Description = 'Updated desc',
  ...
```

**Response** (200 OK):
```json
{
  "id": 1,
  "title": "Updated Title",
  ...
}
```

---

### Endpoint 5: PATCH /api/movies/{id}
```http
PATCH https://localhost:5925/api/movies/1
Content-Type: application/json

{
  "genre": "Crime",
  "durationInMinutes": 160
}
```

**Stored Procedure** (for each field):
```sql
EXEC sp_UpdateMoviePartial 
  @MovieId = 1,
  @FieldName = 'Genre',
  @FieldValue = 'Crime'

EXEC sp_UpdateMoviePartial 
  @MovieId = 1,
  @FieldName = 'DurationInMinutes',
  @FieldValue = '160'
```

**Response** (200 OK):
```json
{
  "id": 1,
  "title": "The Shawshank Redemption",
  "genre": "Crime",
  "durationInMinutes": 160,
  ...
}
```

---

### Endpoint 6: DELETE /api/movies/{id}
```http
DELETE https://localhost:5925/api/movies/1
```

**Stored Procedure**: `sp_DeleteMovie @MovieId = 1`

**Response** (200 OK):
```json
{
  "message": "Movie 'The Shawshank Redemption' deleted successfully."
}
```

---

## Step 4: Testing Endpoints

### Option 1: HTTP File (VS built-in)
```
File: Controllers/movies-crud.http
- Mở file
- Click "Send Request" trên mỗi endpoint
```

### Option 2: Postman
```
1. Import endpoints từ movies-crud.http
2. Set environment: localhost:5925
3. Test từng endpoint
```

### Option 3: cURL (PowerShell)
```powershell
# Get all movies
curl -X GET https://localhost:5925/api/movies --insecure

# Get movie by ID
curl -X GET https://localhost:5925/api/movies/1 --insecure

# Create movie
curl -X POST https://localhost:5925/api/movies `
  -H "Content-Type: application/json" `
  -d '{"title":"New Movie","genre":"Action","durationInMinutes":120,"releaseDate":"2024-01-01"}' `
  --insecure

# Update movie
curl -X PUT https://localhost:5925/api/movies/1 `
  -H "Content-Type: application/json" `
  -d '{"title":"Updated","genre":"Drama","durationInMinutes":130,"releaseDate":"1994-09-23"}' `
  --insecure

# Partial update
curl -X PATCH https://localhost:5925/api/movies/1 `
  -H "Content-Type: application/json" `
  -d '{"genre":"Comedy"}' `
  --insecure

# Delete movie
curl -X DELETE https://localhost:5925/api/movies/1 --insecure
```

---

## Step 5: Error Handling

Tất cả endpoints bắt lỗi và trả về:

### 400 Bad Request
```json
{
  "message": "Title is required.",
  "error": "[error details]"
}
```

### 404 Not Found
```json
{
  "message": "Movie with ID 999 not found."
}
```

### 500 Internal Server Error
```json
{
  "message": "Error retrieving movies.",
  "error": "[SQL or DB exception message]"
}
```

---

## Step 6: Console Logging

Mỗi operation ghi log:

```
[CRUD] GET /api/movies - Retrieved 5 movies
[CRUD] GET /api/movies/1 - Retrieved: The Shawshank Redemption
[CRUD] POST /api/movies - Created: Inception (ID: 3)
[CRUD] PUT /api/movies/1 - Updated: Updated Title
[CRUD] PATCH /api/movies/1 - Partially updated: Updated Title
[CRUD] DELETE /api/movies/1 - Deleted: The Shawshank Redemption
[CRUD] GET /api/movies/999 - Not found (404)
```

---

## Step 7: Production Considerations

### Security
- [ ] Move connection string sang `appsettings.json` + `appsettings.Production.json`
- [ ] Sử dụng environment variables cho sensitive data
- [ ] Thêm authentication/authorization (JWT, OAuth)
- [ ] Input validation + SQL parameter binding (đã có qua Entity Framework)

### Performance
- [ ] Thêm pagination cho `sp_GetAllMovies` (OFFSET/FETCH)
- [ ] Thêm caching (Redis)
- [ ] Indexed columns trong database (Title, Genre, ReleaseDate)
- [ ] Async DB operations (✓ already implemented)

### Logging
- [ ] Replace Console.WriteLine with Serilog/NLog
- [ ] Log vào file hoặc Application Insights
- [ ] Structured logging (JSON format)

### Database
- [ ] Transaction handling cho complex operations
- [ ] Stored procedure versioning
- [ ] Backup strategy

---

## Troubleshooting

### Issue 1: "Keyword not recognized: 'Server=(localdb)\MSSQLLocalDB'"
**Solution**: Kiểm tra connection string format, có thể cần đổi backslash escape:
```
Server=(localdb)\\MSSQLLocalDB
```

### Issue 2: "Invalid object name 'dbo.sp_GetAllMovies'"
**Solution**: 
1. Chạy SQL script để tạo Stored Procedures
2. Kiểm tra database name trong connection string
3. Kiểm tra schema (mặc định: dbo)

### Issue 3: "System.InvalidOperationException: No database provider has been configured"
**Solution**: Kiểm tra Program.cs có `AddDbContext` không

### Issue 4: "ExecuteSqlInterpolated: Incorrect syntax"
**Solution**: Kiểm tra parameter types trong SP vs C# (date format, decimal, etc.)

---

## File References

- **Controller**: `Controllers/MoviesController.cs`
- **Model**: `Models/Movie.cs`
- **DbContext**: `Models/AppDbContext.cs`
- **SQL Scripts**: `SQL/movie-stored-procedures.sql`
- **Test Requests**: `Controllers/movies-crud.http`
- **Config**: `Program.cs` (AppDbContext registration)

---

**Status**: ✅ Fully implemented and ready for testing
