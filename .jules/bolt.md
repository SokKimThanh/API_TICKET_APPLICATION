## 2026-07-06 - EF Core Tracking and Middleware Allocations
**Learning:** In standard CRUD controllers, read-only operations often forget to use `.AsNoTracking()`, which leads to unnecessary memory usage and CPU cycles for change tracking. Additionally, using `_context.Update(entity)` when only a few fields are changed forces an update of all columns in SQL, which is less efficient than letting the change tracker handle it.
**Action:** Always use `.AsNoTracking()` for GET endpoints and avoid redundant `Update()` calls when entities are already tracked.

**Learning:** Middleware that runs on every request should avoid allocations. Creating a new array of search patterns in every call is a common anti-pattern.
**Action:** Move static patterns to `static readonly` fields, and use `SearchValues` (in .NET 9+) for efficient string searching.

## 2026-07-07 - Navigation Property Joins vs. Correlated Subqueries
**Learning:** Using nested/correlated subqueries with `.Contains()` (e.g. `_context.Bookings.Where(...).Select(b => b.Id).Contains(t.BookingId)`) for checking double bookings produces inefficient and complex subqueries in SQL, and misses opportunities for EF Core's built-in relationship joining.
**Action:** Always prefer joining tables using EF Core navigation properties (e.g. `t.Booking.ShowtimeId`) and append `.AsNoTracking()` to avoid any tracking overhead on validator/read-only checks. This compiles to a clean SQL `INNER JOIN` or `LEFT JOIN` which database query planners can optimize much better.

## 2026-07-08 - Highly Optimized Input Validation using SearchValues<string>
**Learning:** Manual loop-based string searching with `string.Contains` over a collection of dangerous patterns is extremely CPU-heavy and allocates excessively. Under .NET 9 and 10, utilizing `SearchValues<string>` allows for vectorized, multi-pattern substring matching.
**Action:** Use `SearchValues.Create(DangerousPatterns, StringComparison.OrdinalIgnoreCase)` combined with `.AsSpan().ContainsAny(Searcher)` to perform lightning-fast, high-performance input scans.

## 2026-08-04 - EF Core Pagination Count Query Optimization
**Learning:** Calling `.CountAsync()` on an Entity Framework Core query that already has `.Include()` and `.ThenInclude()` navigation properties defined causes EF Core to translate and execute complex table joins (and collection joins) even though only a count is needed. This creates significant overhead on SQL Server, especially with large datasets or one-to-many collections.
**Action:** Separate the base filtering query (where clauses) from the projection/include query. Call `.CountAsync()` on the base query, then chain `.Include()` and execute `.ToListAsync()` on the page-projection query to keep the COUNT SQL minimal, fast, and lightweight.
