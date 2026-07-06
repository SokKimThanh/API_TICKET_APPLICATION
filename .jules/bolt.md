## 2026-07-06 - EF Core Tracking and Middleware Allocations
**Learning:** In standard CRUD controllers, read-only operations often forget to use `.AsNoTracking()`, which leads to unnecessary memory usage and CPU cycles for change tracking. Additionally, using `_context.Update(entity)` when only a few fields are changed forces an update of all columns in SQL, which is less efficient than letting the change tracker handle it.
**Action:** Always use `.AsNoTracking()` for GET endpoints and avoid redundant `Update()` calls when entities are already tracked.

**Learning:** Middleware that runs on every request should avoid allocations. Creating a new array of search patterns in every call is a common anti-pattern.
**Action:** Move static patterns to `static readonly` fields, and use `SearchValues` (in .NET 9+) for efficient string searching.
