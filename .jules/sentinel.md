## 2025-05-15 - Information Leakage and Hardcoded Connection Strings
**Vulnerability:** Raw exception messages (`ex.Message`) were returned to the client in `MoviesController`, and a plaintext connection string was hardcoded in `AppDbContext.OnConfiguring`.
**Learning:** Scaffolding often generates `OnConfiguring` with a hardcoded connection string as a fallback, which developers may forget to remove. Standard error handling templates sometimes default to exposing exception details for "convenience" during initial development.
**Prevention:** Always remove hardcoded configuration from DbContext after setup. Use a global response wrapper or standard generic error messages in controllers to prevent internal data exposure.
