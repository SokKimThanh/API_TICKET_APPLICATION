# Hướng Dẫn Chi Tiết Các Stored Procedures

> **Tài liệu này giải thích từng Stored Procedure (SP) trong `SQL/movie-stored-procedures.sql`**
> **Mục đích: Giúp developer hiểu logic, cách sử dụng, lỗi có thể gặp**

---

## Mục Lục
1. sp_GetAllMovies - Lấy tất cả phim
2. sp_GetMovieById - Lấy phim theo ID
3. sp_GetMovieByTitle - Lấy phim theo tên
4. sp_CreateMovie - Tạo phim mới
5. sp_UpdateMovie - Cập nhật toàn bộ phim
6. sp_UpdateMoviePartial - Cập nhật 1 trường phim
7. sp_DeleteMovie - Xóa phim

---

## 1. sp_GetAllMovies - Lấy Tất Cả Phim

### Mục Đích
- Truy vấn danh sách toàn bộ phim từ bảng `Movies`
- Sắp xếp theo tên phim (A → Z)
- Không có điều kiện lọc

### Cú Pháp
```sql
EXEC sp_GetAllMovies;
```

### Tham Số
- **Không có tham số**

### Kết Quả Trả Về
| Trường | Kiểu | Ghi Chú |
|--------|------|--------|
| Id | INT | ID phim (khóa chính) |
| Title | NVARCHAR(255) | Tên phim |
| Description | TEXT | Mô tả phim |
| Genre | NVARCHAR(100) | Thể loại phim |
| DurationInMinutes | INT | Thời lượng (phút) |
| PosterUrl | NVARCHAR(255) | URL hình poster |
| ReleaseDate | DATE | Ngày phát hành |

### Kết Quả JSON Ví Dụ
```json
[
  {
    "id": 1,
    "title": "The Shawshank Redemption",
    "description": "Two imprisoned men bond...",
    "genre": "Drama",
    "durationInMinutes": 142,
    "posterUrl": "https://example.com/poster.jpg",
    "releaseDate": "1994-09-23"
  },
  {
    "id": 2,
    "title": "The Dark Knight",
    "description": "Batman faces the Joker...",
    "genre": "Action",
    "durationInMinutes": 152,
    "posterUrl": "https://example.com/knight.jpg",
    "releaseDate": "2008-07-18"
  }
]
```

### HTTP Request (từ Controller)
```http
GET /api/movies
```

### Lỗi Có Thể Gặp
| Lỗi | Nguyên Nhân | Giải Pháp |
|-----|-----------|---------|
| "Timeout" | Bảng Movies quá lớn | Thêm pagination (OFFSET/FETCH NEXT) |
| Empty result | Không có phim | Dữ liệu chưa được insert |
| Connection error | Database offline | Kiểm tra SQL Server |

### Lời Khuyên Hiệu Năng
- ⚠️ **Vấn đề**: Nếu Movies có 100,000+ dòng, query sẽ chậm
- ✅ **Giải pháp**: Thêm **pagination**
```sql
-- Ví dụ: Lấy 10 phim, trang 1
SELECT ... FROM Movies 
ORDER BY Title ASC
OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;
```

---

## 2. sp_GetMovieById - Lấy Phim Theo ID

### Mục Đích
- Tìm kiếm một phim cụ thể bằng ID
- Dùng khi controller cần lấy chi tiết 1 phim
- Áp dụng điều kiện WHERE (Id = @MovieId)

### Cú Pháp
```sql
EXEC sp_GetMovieById @MovieId = 1;
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Ghi Chú |
|---------|------|---------|--------|
| @MovieId | INT | ✅ Có | ID của phim cần tìm |

### Kết Quả Trả Về
- **Nếu tìm thấy**: 1 bản ghi phim
- **Nếu không tìm thấy**: Tập rỗng (0 dòng)

### Ví Dụ Thành Công
```sql
EXEC sp_GetMovieById @MovieId = 1;
```

**Kết quả**:
```
Id      Title                      Description              Genre   Duration  ...
1       The Shawshank Redemption   Two imprisoned men...   Drama   142       ...
```

### Ví Dụ Không Tìm Thấy
```sql
EXEC sp_GetMovieById @MovieId = 999;
```

**Kết quả**: (Không có kết quả, 0 dòng)

```csharp
// Controller xử lý:
var movie = await _context.Movies
    .FromSqlInterpolated($"EXEC sp_GetMovieById {id}")
    .FirstOrDefaultAsync();

if (movie == null)
    return NotFound(new { message = "Movie not found" }); // 404
```

### HTTP Request
```http
GET /api/movies/1      → 200 OK + JSON
GET /api/movies/999    → 404 Not Found
```

### Lỗi Có Thể Gặp
| Lỗi | Nguyên Nhân | Giải Pháp |
|-----|-----------|---------|
| "Invalid @MovieId" | Tham số không phải số | Kiểm tra controller validation |
| Empty result | ID không tồn tại | Trả 404 từ controller |

### Hiệu Năng
- **Độ phức tạp**: O(log n) - sử dụng index PK
- **Rất nhanh** ✅ ngay cả với bảng 1 triệu dòng
- **Index**: `Id` là khóa chính (PK_Movies__3214EC077240C02B) tự động indexed

---

## 3. sp_GetMovieByTitle - Lấy Phim Theo Tên

### Mục Đích
- Tìm phim dựa trên **tên phim** (Title)
- Dùng sau khi insert (lấy ID vừa tạo)
- Nếu có nhiều phim cùng tên: lấy phim gần nhất (TOP 1, ORDER BY Id DESC)

### Cú Pháp
```sql
EXEC sp_GetMovieByTitle @Title = 'Inception';
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Ghi Chú |
|---------|------|---------|--------|
| @Title | NVARCHAR(255) | ✅ Có | Tên phim cần tìm |

### Kết Quả Trả Về
- **TOP 1**: Chỉ 1 kết quả
- **ORDER BY Id DESC**: Phim được tạo gần nhất

### Ví Dụ: Sau Khi Insert Phim Mới
```csharp
// Controller:
// 1. Gọi sp_CreateMovie
await _context.Database.ExecuteSqlInterpolatedAsync(
    $"EXEC sp_CreateMovie {movie.Title}, {movie.Description}, ..."
);

// 2. Gọi sp_GetMovieByTitle để lấy ID vừa tạo
var createdMovie = await _context.Movies
    .FromSqlInterpolated($"EXEC sp_GetMovieByTitle {movie.Title}")
    .FirstOrDefaultAsync();

return CreatedAtAction(nameof(GetById), new { id = createdMovie?.Id }, createdMovie);
```

### Lỗi Có Thể Gặp
| Lỗi | Nguyên Nhân | Giải Pháp |
|-----|-----------|---------|
| NULL result | Tên phim không tồn tại | Kiểm tra insert thành công? |
| Case-sensitive mismatch | Tên không khớp chính xác | Sử dụng COLLATE hoặc trim |
| Performance slow | Không có index trên Title | Tạo index (xem bên dưới) |

### Tạo Index Để Tăng Tốc
```sql
-- Chạy trong SSMS một lần
CREATE INDEX idx_Movies_Title ON Movies(Title);
```

---

## 4. sp_CreateMovie - Tạo Phim Mới

### Mục Đích
- Thêm phim mới vào bảng Movies
- Kiểm tra validation (Title không được rỗng)
- Xử lý exception với BEGIN TRY/CATCH

### Cú Pháp
```sql
EXEC sp_CreateMovie 
    @Title = 'Inception',
    @Description = 'A thief who steals...',
    @Genre = 'Sci-Fi',
    @DurationInMinutes = 148,
    @PosterUrl = 'https://example.com/inception.jpg',
    @ReleaseDate = '2010-07-16';
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Mặc Định | Ghi Chú |
|---------|------|---------|----------|--------|
| @Title | NVARCHAR(255) | ✅ | - | Tên phim (không được rỗng) |
| @Description | NVARCHAR(MAX) | ❌ | NULL | Mô tả phim |
| @Genre | NVARCHAR(100) | ✅ | - | Thể loại phim |
| @DurationInMinutes | INT | ✅ | - | Thời lượng (phút) |
| @PosterUrl | NVARCHAR(255) | ❌ | NULL | URL poster |
| @ReleaseDate | DATE | ✅ | - | Ngày phát hành |

### Kết Quả Trả Về
```
(1 row affected)
Phim được tạo thành công. ID: 3
```

### HTTP Request
```http
POST /api/movies
Content-Type: application/json

{
  "title": "Inception",
  "description": "A thief who steals...",
  "genre": "Sci-Fi",
  "durationInMinutes": 148,
  "posterUrl": "https://example.com/inception.jpg",
  "releaseDate": "2010-07-16"
}
```

### HTTP Response (201 Created)
```json
{
  "id": 3,
  "title": "Inception",
  "description": "A thief who steals...",
  "genre": "Sci-Fi",
  "durationInMinutes": 148,
  "posterUrl": "https://example.com/inception.jpg",
  "releaseDate": "2010-07-16"
}
```

### Lỗi Có Thể Gặp
| Lỗi | Status | Nguyên Nhân | Giải Pháp |
|-----|--------|-----------|---------|
| "Title không được để trống" | 400 | @Title rỗng | Kiểm tra controller validation |
| "Unique constraint" | 400 | Title trùng (nếu có constraint) | Đổi Title khác |
| "Foreign key violation" | 400 | Genre không tồn tại (nếu có FK) | Kiểm tra Genre valid |
| Exception | 500 | SQL error | Kiểm tra SSMS logs |

### Validation Trong SP
```sql
-- Kiểm tra Title
IF @Title IS NULL OR LEN(LTRIM(@Title)) = 0
BEGIN
    THROW 50002, 'Title không được để trống', 1;
END
```

### Lưu Ý
- ⚠️ **SCOPE_IDENTITY()**: Lấy ID vừa insert (auto-increment)
- ✅ **BEGIN TRY/CATCH**: Bắt lỗi và throw cho Controller xử lý
- 📌 **Controller kiểm tra**: Title bắt buộc trước khi gọi SP

---

## 5. sp_UpdateMovie - Cập Nhật Toàn Bộ Phim

### Mục Đích
- Cập nhật **TẤT CẢ** trường của phim (PUT request)
- Yêu cầu phim phải tồn tại (ID hợp lệ)
- Kiểm tra @@ROWCOUNT (số dòng bị update)

### Cú Pháp
```sql
EXEC sp_UpdateMovie
    @MovieId = 1,
    @Title = 'Updated Title',
    @Description = 'Updated description',
    @Genre = 'Drama',
    @DurationInMinutes = 150,
    @PosterUrl = 'https://example.com/new-poster.jpg',
    @ReleaseDate = '1994-09-23';
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Ghi Chú |
|---------|------|---------|--------|
| @MovieId | INT | ✅ | ID phim cần update |
| @Title | NVARCHAR(255) | ✅ | Tên mới |
| @Description | NVARCHAR(MAX) | ❌ | Mô tả mới (NULL cho skip) |
| @Genre | NVARCHAR(100) | ✅ | Thể loại mới |
| @DurationInMinutes | INT | ✅ | Thời lượng mới |
| @PosterUrl | NVARCHAR(255) | ❌ | URL poster mới |
| @ReleaseDate | DATE | ✅ | Ngày phát hành mới |

### Kết Quả Trả Về
```
(1 row affected)
Phim được cập nhật thành công.
```

### HTTP Request (PUT)
```http
PUT /api/movies/1
Content-Type: application/json

{
  "title": "Updated Title",
  "description": "Updated description",
  "genre": "Drama",
  "durationInMinutes": 150,
  "posterUrl": "https://example.com/new-poster.jpg",
  "releaseDate": "1994-09-23"
}
```

### HTTP Response (200 OK)
```json
{
  "id": 1,
  "title": "Updated Title",
  "description": "Updated description",
  "genre": "Drama",
  "durationInMinutes": 150,
  "posterUrl": "https://example.com/new-poster.jpg",
  "releaseDate": "1994-09-23"
}
```

### Lỗi Có Thể Gặp
| Lỗi | Status | Nguyên Nhân | Giải Pháp |
|-----|--------|-----------|---------|
| "Phim không tồn tại" | 404 | ID không tồn tại | Kiểm tra ID, use PUT vs POST |
| @@ROWCOUNT = 0 | 404 | WHERE Id=@MovieId không match | ID sai |
| Exception | 500 | SQL error | Kiểm trace error |

### Hiệu Năng
```sql
-- @@ROWCOUNT: Số dòng bị ảnh hưởng
IF @@ROWCOUNT = 0
BEGIN
    THROW 50001, 'Phim không tồn tại', 1;
END
```

### Khác Biệt: PUT vs PATCH
| Điều | PUT (sp_UpdateMovie) | PATCH (sp_UpdateMoviePartial) |
|-----|----------------------|-------------------------------|
| Update | TẤT CẢ fields | 1 field cụ thể |
| Tham số | Tất cả fields | Chỉ 1 field |
| Yêu cầu đầu vào | Đầy đủ dữ liệu | Dữ liệu bộ phận |
| HTTP Method | PUT /api/movies/1 | PATCH /api/movies/1 |
| Use case | Thay thế toàn bộ | Fix 1 lỗi |

---

## 6. sp_UpdateMoviePartial - Cập Nhật 1 Trường (PATCH)

### Mục Đích
- Cập nhật **CHỈ MỘT TRƯỜNG** của phim (PATCH request)
- Không cần biết toàn bộ dữ liệu, chỉ update 1 field
- Sử dụng **Dynamic SQL** để linh hoạt

### Cú Pháp
```sql
-- Cập nhật chỉ Genre
EXEC sp_UpdateMoviePartial
    @MovieId = 1,
    @FieldName = 'Genre',
    @FieldValue = 'Comedy';

-- Cập nhật chỉ DurationInMinutes
EXEC sp_UpdateMoviePartial
    @MovieId = 1,
    @FieldName = 'DurationInMinutes',
    @FieldValue = '165';
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Ghi Chú |
|---------|------|---------|--------|
| @MovieId | INT | ✅ | ID phim cần update |
| @FieldName | NVARCHAR(100) | ✅ | Tên trường (VD: 'Genre', 'Title', ...) |
| @FieldValue | NVARCHAR(MAX) | ✅ | Giá trị mới |

### Trường Hợp Hỗ Trợ
```
Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate
```

### HTTP Request (PATCH)
```http
PATCH /api/movies/1
Content-Type: application/json

{
  "genre": "Comedy",
  "durationInMinutes": 165
}
```

**Lưu ý**: Controller gọi sp_UpdateMoviePartial **2 lần** (1 cho mỗi field)

### HTTP Response (200 OK)
```json
{
  "id": 1,
  "title": "The Shawshank Redemption",
  "description": "...",
  "genre": "Comedy",
  "durationInMinutes": 165,
  ...
}
```

### Dynamic SQL Explanation
```sql
-- @FieldName = 'Genre'
-- @FieldValue = 'Comedy'
-- @MovieId = 1

-- Xây dựng câu SQL:
SET @SQL = 'UPDATE Movies SET ' + QUOTENAME(@FieldName) + ' = @Value WHERE Id = @MovieId';
-- Kết quả: 'UPDATE Movies SET [Genre] = @Value WHERE Id = @MovieId'

-- Thực thi:
EXEC sp_executesql 
    @SQL, 
    N'@Value NVARCHAR(MAX), @MovieId INT', 
    @Value = @FieldValue, 
    @MovieId = @MovieId;
```

### Bảo Vệ SQL Injection
- ✅ **QUOTENAME()**: Bảo vệ tên trường (VD: 'Genre' → [Genre])
- ✅ **sp_executesql + @Value parameter**: Bảo vệ giá trị (tránh injection)

```sql
-- ❌ Không an toàn:
SET @SQL = 'UPDATE Movies SET ' + @FieldName + ' = ''' + @FieldValue + '''';

-- ✅ An toàn:
SET @SQL = 'UPDATE Movies SET ' + QUOTENAME(@FieldName) + ' = @Value WHERE Id = @MovieId';
EXEC sp_executesql @SQL, N'@Value NVARCHAR(MAX)', @Value = @FieldValue;
```

### Lỗi Có Thể Gặp
| Lỗi | Status | Nguyên Nhân | Giải Pháp |
|-----|--------|-----------|---------|
| "Phim không tồn tại" | 404 | ID không tồn tại | Kiểm tra ID |
| Invalid column error | 400 | @FieldName không tồn tại | Kiểm tra tên trường |
| Type mismatch | 400 | VD: 'abc' cho DurationInMinutes (INT) | Validate type trước gửi |

### Ví Dụ Thực Tế (Controller)
```csharp
// PATCH /api/movies/1
[HttpPatch("{id}")]
public async Task<IActionResult> PartialUpdate(int id, [FromBody] Dictionary<string, object> updates)
{
    foreach (var update in updates)
    {
        var value = update.Value?.ToString() ?? "";
        // Gọi SP cho mỗi field
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_UpdateMoviePartial {id}, {update.Key}, {value}"
        );
    }
    // Reload và trả về
}
```

---

## 7. sp_DeleteMovie - Xóa Phim

### Mục Đích
- Xóa phim khỏi bảng Movies (Hard delete - xóa vĩnh viễn)
- Kiểm tra phim tồn tại trước xóa
- Xử lý lỗi Foreign Key constraint

### Cú Pháp
```sql
EXEC sp_DeleteMovie @MovieId = 1;
```

### Tham Số
| Tham Số | Kiểu | Bắt Buộc | Ghi Chú |
|---------|------|---------|--------|
| @MovieId | INT | ✅ | ID phim cần xóa |

### Kết Quả Trả Về
```
(1 row affected)
Phim được xóa thành công.
```

### HTTP Request (DELETE)
```http
DELETE /api/movies/1
```

### HTTP Response (200 OK)
```json
{
  "message": "Movie 'The Shawshank Redemption' deleted successfully."
}
```

### Lỗi Có Thể Gặp
| Lỗi | Status | Nguyên Nhân | Giải Pháp |
|-----|--------|-----------|---------|
| "Phim không tồn tại" | 404 | ID không tồn tại | Kiểm tra ID |
| Foreign Key constraint | 400 | Có Showtime/Booking tham chiếu phim | Xóa dữ liệu tham chiếu trước, hoặc dùng CASCADE DELETE |
| Permission denied | 403 | User không có quyền xóa | Kiểm tra authorization |

### FK Constraint Ví Dụ
```sql
-- Lỗi có thể gặp:
-- "The DELETE statement conflicted with a FOREIGN KEY constraint 
--  "FK_Showtimes_Movies". The conflict occurred in database 
--  "TicketManagementDB", table "dbo.Showtimes", column 'MovieId'."
```

### Giải Pháp
1. **Xóa dữ liệu tham chiếu trước**
```sql
-- Xóa tất cả Showtimes của phim
DELETE FROM Showtimes WHERE MovieId = 1;
-- Sau đó xóa phim
EXEC sp_DeleteMovie @MovieId = 1;
```

2. **Thêm CASCADE DELETE vào FK** (tùy chọn, cần cập nhật schema)
```sql
ALTER TABLE Showtimes
DROP CONSTRAINT FK_Showtimes_Movies;

ALTER TABLE Showtimes
ADD CONSTRAINT FK_Showtimes_Movies
    FOREIGN KEY (MovieId) REFERENCES Movies(Id)
    ON DELETE CASCADE; -- Tự động xóa Showtimes khi xóa Movie
```

3. **Soft Delete** (thay vì xóa, đánh dấu đã xóa)
```sql
-- Thêm column IsDeleted
ALTER TABLE Movies ADD IsDeleted BIT DEFAULT 0;

-- Modify SP: UPDATE IsDeleted = 1 instead of DELETE
UPDATE Movies SET IsDeleted = 1 WHERE Id = @MovieId;
```

### Hiệu Năng
- ✅ Nhanh O(1) cho single delete
- ⚠️ Có thể chậm nếu phải xóa nhiều FK records

---

## Bảng So Sánh Tất Cả SP

| SP | HTTP | Tác Vụ | Tham Số | Kết Quả |
|----|------|-------|--------|---------|
| sp_GetAllMovies | GET /api/movies | Lấy all | Không | List movies |
| sp_GetMovieById | GET /api/movies/1 | Lấy 1 | @MovieId | 1 movie hoặc ∅ |
| sp_GetMovieByTitle | (internal) | Lấy by Title | @Title | 1 movie hoặc ∅ |
| sp_CreateMovie | POST /api/movies | Tạo mới | @Title, ... | ✅ Success/400 |
| sp_UpdateMovie | PUT /api/movies/1 | Update all | @MovieId, ... | ✅ Success/404 |
| sp_UpdateMoviePartial | PATCH /api/movies/1 | Update 1 field | @MovieId, @FieldName, @FieldValue | ✅ Success/404 |
| sp_DeleteMovie | DELETE /api/movies/1 | Xóa | @MovieId | ✅ Success/404/400 |

---

## Error Codes Tùy Chỉnh

```sql
-- SP sử dụng THROW với custom error codes:
THROW 50001, 'Phim không tồn tại', 1;     -- 50001 = Not Found
THROW 50002, 'Title không được để trống', 1; -- 50002 = Validation Error
```

---

## Chạy Test Trong SSMS

1. Mở SQL Server Management Studio
2. Kết nối tới: `(localdb)\MSSQLLocalDB`
3. Chọn DB: `TicketManagementDB`
4. Chạy script `SQL/movie-stored-procedures.sql` để tạo SPs
5. Bỏ comment các lệnh test ở cuối file
6. Chạy từng lệnh để kiểm tra

---

## API Testing (Postman / HTTP File)

File: `Controllers/movies-crud.http`

```http
# 1. Get All
GET https://localhost:5925/api/movies

# 2. Get By ID
GET https://localhost:5925/api/movies/1

# 3. Create
POST https://localhost:5925/api/movies
Content-Type: application/json

{ "title": "...", ... }

# 4. Update (PUT)
PUT https://localhost:5925/api/movies/1
Content-Type: application/json

{ "title": "...", ... }

# 5. Update (PATCH)
PATCH https://localhost:5925/api/movies/1
Content-Type: application/json

{ "genre": "..." }

# 6. Delete
DELETE https://localhost:5925/api/movies/1
```

---

**Tài liệu được cập nhật lần cuối**: Hôm nay  
**Phiên bản**: 1.0  
**Trạng thái**: ✅ Ready for production
