## 2025-05-15 - Information Leakage and Hardcoded Connection Strings
**Vulnerability:** Raw exception messages (`ex.Message`) were returned to the client in `MoviesController`, and a plaintext connection string was hardcoded in `AppDbContext.OnConfiguring`.
**Learning:** Scaffolding often generates `OnConfiguring` with a hardcoded connection string as a fallback, which developers may forget to remove. Standard error handling templates sometimes default to exposing exception details for "convenience" during initial development.
**Prevention:** Always remove hardcoded configuration from DbContext after setup. Use a global response wrapper or standard generic error messages in controllers to prevent internal data exposure.

## 2026-07-28 - Reusable Security Pattern for API Security Headers and Conditional CSP
**Vulnerability:** Lack of defense-in-depth HTTP headers such as Clickjacking, MIME-type sniffing, Referrer Leakage, and Cross-Site Scripting (XSS) protections.
**Learning:** Adding standard strict `Content-Security-Policy` directly can break web-based API documentation tools (like Swagger UI) which rely on inline styles/scripts.
**Prevention:** Group standard security headers for all endpoints, but restrict the strict `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'; sandbox` exclusively to `/api` routes to safeguard raw JSON output from being framed or executing HTML/JS.
