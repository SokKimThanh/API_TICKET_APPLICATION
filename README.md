# API Ticket Application

A professional **Database-First Movie Booking API** built with **ASP.NET Core (.NET 10)**, featuring secure role-based authorization, transactional booking workflows, and comprehensive REST endpoints with Swagger/OpenAPI documentation.

**Current Version:** `v1.1.0-mvp` (Phase 1 - Core MVP Completed) ✅

---

## Project Overview

The **API Ticket Application** is a backend API system for a movie ticket booking platform. It manages users, movies, showtimes, seats, bookings, and tickets with enterprise-grade security and data integrity. The system prevents double-booking through database transactions and enforces strict role-based access control (Admin vs. User) with ownership validation.

### Key Characteristics
- **Framework:** .NET 10 with C# 14
- **Database:** SQL Server (LocalDB / Express)
- **Architecture:** REST API with standardized response models
- **Security:** JWT-based authentication, role-based authorization, transaction support
- **API Documentation:** Swagger/OpenAPI with full HTTP status code documentation
- **Testing:** Modular `.http` scripts for automated REST client testing

---

## Current Progress (Phase 1 - MVP Completed) ✅

### Implemented Features

#### 1. **Standardized Response Models**
- ✅ `ResponseModel<T>` — unified wrapper for all API responses
- ✅ `PagedData<T>` — standardized pagination support across all controllers
- ✅ Consistent HTTP status codes: `200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `500 Internal Server Error`

#### 2. **Database Transactions & Booking Flow**
- ✅ Transaction support for `Bookings` and `Tickets` generation
- ✅ Prevents double-booking through row-level locking and transaction isolation
- ✅ Atomic operations ensuring data consistency across related entities

#### 3. **Secure Role-Based Authorization**
- ✅ Admin vs. User role enforcement on sensitive endpoints
- ✅ Strict ownership validation (users can only modify their own bookings/tickets)
- ✅ JWT token support with claim-based authorization
- ✅ Endpoint-level `[Authorize]` and `[Authorize(Roles = "Admin")]` attributes

#### 4. **Swagger/OpenAPI Documentation**
- ✅ Full API documentation with HTTP status codes
- ✅ Request/Response model definitions with examples
- ✅ Authorization header configuration for JWT tokens
- ✅ Interactive Swagger UI at `/swagger` endpoint

#### 5. **Modular Testing Infrastructure**
- ✅ `.http` request scripts in `RequestHTTP/` directory
- ✅ Organized by feature (users, movies, showtimes, bookings, tickets)
- ✅ Ready for automated REST client testing in Visual Studio or Postman
- ✅ Pre-configured headers and authentication tokens

#### 6. **Base Controller Pattern**
- ✅ Abstract `AppBaseController` for code reusability
- ✅ Centralized response generation (`OkResponse`, `ErrorResponse`, `CreatedResponse`)
- ✅ Helper methods for JWT claims (`GetUserId()`, `GetUserEmail()`, `IsUserInRole()`)
- ✅ Database context management across all controllers

---

## Current Database Architecture

The system uses **6 core entities** organized in a relational model:

| Entity | Purpose | Key Fields |
|--------|---------|-----------|
| **Users** | System users (Admin, Customer) | UserId, Email, PasswordHash, Role, CreatedAt |
| **Movies** | Movie catalog | MovieId, Title, Description, Duration, ReleaseDate |
| **Showtimes** | Movie screening schedules | ShowtimeId, MovieId, StartTime, EndTime, TotalSeats |
| **Seats** | Individual theater seats | SeatId, ShowtimeId, SeatNumber, IsAvailable |
| **Bookings** | Customer booking records | BookingId, UserId, ShowtimeId, BookingDate, TotalPrice |
| **Tickets** | Individual tickets per booking | TicketId, BookingId, SeatId, IsUsed |

### Relationships
- `Users` (1) → (many) `Bookings`
- `Movies` (1) → (many) `Showtimes`
- `Showtimes` (1) → (many) `Seats` & (many) `Bookings`
- `Bookings` (1) → (many) `Tickets`
- `Seats` (1) → (many) `Tickets`

**Note:** Visual cleanup is planned to remove redundant `CreatedBy`/`UpdatedBy` columns from the diagram in Phase 2 for improved readability.

---

## Core Endpoints (Phase 1)

### Authentication
- `POST /api/auth/login` — User login, returns JWT token
- `POST /api/auth/register` — User registration

### Movies
- `GET /api/movies?pageNumber={page}&pageSize={size}` — List all movies with pagination
- `GET /api/movies/{id}` — Get movie details by ID
- `POST /api/movies` — Create new movie (Admin only)
- `PUT /api/movies/{id}` — Update movie (Admin only)
- `PATCH /api/movies/{id}` — Partially update movie (Admin only)
- `DELETE /api/movies/{id}` — Delete movie (Admin only)

### Showtimes
- `GET /api/showtimes?movieId={id}&pageNumber={page}` — List showtimes
- `GET /api/showtimes/{id}` — Get showtime details
- `POST /api/showtimes` — Create showtime (Admin only)
- `PUT /api/showtimes/{id}` — Update showtime (Admin only)
- `DELETE /api/showtimes/{id}` — Delete showtime (Admin only)

### Seats
- `GET /api/seats?showtimeId={id}` — Get available seats for showtime
- `GET /api/seats/{id}` — Get seat details

### Bookings & Tickets
- `POST /api/bookings` — Create booking (transactional, generates tickets)
- `GET /api/bookings/{id}` — Get user's booking details (ownership validated)
- `GET /api/bookings/user/{userId}` — List user's bookings (Admin or self)
- `GET /api/tickets/{bookingId}` — Get tickets for a booking (ownership validated)

### Response Format
All API responses follow this standardized format:
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* response data */ },
  "timestamp": "2024-12-15T10:30:45Z",
  "statusCode": 200
}
```

---

## Installation & Setup

### Prerequisites
- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download)
- **SQL Server** — LocalDB, Express, or Full Edition
- **Visual Studio 2026** or **VS Code**
- **Git** — For repository cloning

### Quick Start

1. **Clone the repository:**
   ```bash
   git clone https://github.com/SokKimThanh/API_TICKET_APPLICATION.git
   cd API_TICKET_APPLICATION
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure database connection:**
   - Open `appsettings.json` and set your connection string:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
     }
     ```

4. **Create and seed the database:**
   ```bash
   dotnet ef database update
   ```

5. **Setup HTTPS certificate (if needed):**
   ```bash
   dotnet dev-certs https --trust
   ```

6. **Run the application:**
   ```bash
   dotnet run
   ```

   The API will be available at:
   - **HTTP:** `http://localhost:5924` (redirects to HTTPS)
   - **HTTPS:** `https://localhost:5925`
   - **Swagger UI:** `https://localhost:5925/swagger/index.html`

---

## Testing the API

### Using .HTTP Scripts
Visual Studio includes a REST Client for testing `.http` files. Open any script in the `RequestHTTP/` directory and click "Send Request":

```http
### Get all movies (paginated)
GET https://localhost:5925/api/movies?pageNumber=1&pageSize=10
Authorization: Bearer {your-jwt-token}
```

### Using Postman
1. Import the `.http` files from `RequestHTTP/` directory
2. Set your JWT token in the Authorization header
3. Execute requests to test all endpoints

### Using cURL
```bash
# Get all movies
curl -X GET "https://localhost:5925/api/movies?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -k  # Skip SSL verification for local dev
```

---

## Project Structure

```
API_TICKET_APPLICATION/
├── Controllers/              # API endpoints
│   ├── AppBaseController.cs  # Abstract base for all controllers
│   ├── AuthController.cs     # Authentication endpoints
│   ├── MoviesController.cs   # Movie CRUD operations
│   ├── ShowtimesController.cs # Showtime management
│   ├── SeatsController.cs    # Seat queries
│   ├── BookingsController.cs # Booking & ticketing
│   └── TicketsController.cs  # Ticket management
├── Models/                   # Data models & DbContext
│   ├── AppDbContext.cs       # Entity Framework Core DbContext
│   ├── User.cs, Movie.cs, Showtime.cs, Seat.cs, Booking.cs, Ticket.cs
│   └── ResponseModel.cs, PagedData.cs  # Response wrappers
├── Services/                 # Business logic
│   ├── BookingService.cs     # Booking & ticket generation (transactional)
│   └── AuthService.cs        # JWT token generation
├── RequestHTTP/              # REST client test scripts
│   ├── movies.http
│   ├── showtimes.http
│   ├── bookings.http
│   ├── tickets.http
│   └── auth.http
├── Migrations/               # EF Core migrations
├── Program.cs                # Application startup & DI configuration
├── appsettings.json          # Configuration
└── README.md                 # This file
```

---

## Security Features

### Authentication & Authorization
- **JWT-based authentication** with role claims
- **Role-based access control** (Admin, User)
- **Ownership validation** — Users cannot access other users' data
- **HTTPS enforcement** with certificate pinning option
- **CORS configuration** for controlled cross-origin access

### Data Protection
- **Password hashing** using bcrypt/PBKDF2
- **Transactional integrity** for bookings and ticket generation
- **SQL injection prevention** via parameterized queries
- **XSS protection** through proper encoding
- **Input validation** on all endpoints

---

## Next Roadmap (Phase 2 - Upcoming)

### Phase 2 Goals: Multi-Theater Expansion

In Phase 2, we will extend the system to support **multi-theater business logic** with the following additions:

#### New Entities
- **Cinemas** — Theater chain/location management
  - CinemaId, Name, Location, City, Country, ContactInfo

- **CinemaHalls** — Individual auditoriums within cinemas
  - HallId, CinemaId, Name, Capacity, LayoutConfig

#### Data Model Updates
- `Showtimes` will be linked to `CinemaHalls` (instead of generic "theater")
- `Seats` architecture will support dynamic hall layouts
- Booking system will handle cross-cinema reservations

#### API Enhancements
- Cinema discovery and management endpoints
- Hall availability and capacity management
- Enhanced filtering (search by city, cinema location)
- Multi-theater reporting and analytics

#### Benefits
- Support for cinema chains and franchises
- Regional theater management
- Improved scalability and real-world applicability
- Foundation for future features (loyalty programs, corporate bookings, etc.)

**Estimated Timeline:** Q1 2025

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Connection refused on `localhost:5924` | Ensure the application is running and Kestrel is bound to port 5924 |
| SSL/TLS certificate errors | Run `dotnet dev-certs https --trust` to trust the development certificate |
| Database not found | Check `appsettings.json` connection string and ensure SQL Server is running |
| Unauthorized (401) on protected endpoints | Verify JWT token is passed in `Authorization: Bearer {token}` header |
| Forbidden (403) on admin endpoints | Ensure user has `Admin` role claim in JWT token |
| Swagger UI not loading | Navigate to `https://localhost:5925/swagger` and ensure HTTPS is used |

---

## Contributing

We welcome contributions! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/my-feature`
3. **Commit** your changes: `git commit -m "Add my feature"`
4. **Push** to the branch: `git push origin feature/my-feature`
5. **Submit** a Pull Request with a clear description

### Code Standards
- Follow C# naming conventions (PascalCase for public members, camelCase for locals)
- Use `async/await` for I/O operations
- Include XML documentation comments for public APIs
- Write unit tests for new business logic
- Ensure all tests pass before submitting PR

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## Contact & Resources

- **Repository:** [https://github.com/SokKimThanh/API_TICKET_APPLICATION](https://github.com/SokKimThanh/API_TICKET_APPLICATION)
- **Issues & Bug Reports:** [GitHub Issues](https://github.com/SokKimThanh/API_TICKET_APPLICATION/issues)
- **Documentation:** See `Docs/` directory for detailed guides

---

**Made with ❤️ for modern API development**