ProductOrderAPI

A Clean Architecture–based ASP.NET Core Web API for managing products, orders, users, and authentication.
It demonstrates best practices including Entity Framework Core, JWT authentication, role-based authorization, auditing, concurrency handling, and secure password storage with BCrypt.

Features

Product Catalog

  * Add, update, delete, and view products.
  * Supports stock quantity management.

Order Management

  * Place orders with one or more products.
  * Order validation (fails gracefully if insufficient stock).
  * Order items cascade deletion.

User Authentication & Authorization

  * JWT-based authentication with claims:

    * `username`
    * `role`
    * `DateLogin`
    * `TimeLogin`
  * Secure password hashing with BCrypt.
  * Role-based access (Admin / User).

Audit Logs

  * Tracks user activities for accountability.

Concurrency Handling

  * Optimistic concurrency with `RowVersion` on products.

Architecture

This project follows Clean Architecture principles:

ProductOrderAPI
│
├── Domain              # Entities and core models
├── Application         # Interfaces, DTOs 
├── Infrastructure      # EF Core, Repositories, Security, Services
│   ├── Persistence     # AppDbContext, Migrations, Seed data
│   └── Security        # JWT & Password Hashing
├── Shared				          # Middleware	
├── API                 # Controllers, Endpoints
└── Test                # xUnit

Tech Stack

* .NET 9 / ASP.NET Core Web API
* Entity Framework Core (SQL Server)
* JWT Authentication
* BCrypt.Net for password hashing
* Clean Architecture principles
* xUnit


Authentication
Login
```json
{
  "username": "admin",
  "password": "admin123"
}
```
{
  "username": "user",
  "password": "user123"
}
```

JWT tokens include user details and login metadata:

```json
{
  "unique_name": "admin",
  "role": "Admin",
  "DateLogin": "2025-09-02",
  "TimeLogin": "18:45:23",
  "exp": 1693679123,
  "iss": "your-app",
  "aud": "your-app-users"
}
```

API response when logging in:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR...",
  "username": "admin",
  "role": "Admin",
  "dateTime": "2025-09-02T18:45:23Z"
}
```

---

Setup & Installation

1. Clone repository

   ```bash
   git clone https://github.com/phemzyadex/ProductOrderAPI
   cd ProductOrderAPI
   ```

2. Update database connection
   Edit `appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.;Database=ProductOrderDB;User Id=sa;Password=*****;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Pooling=True;Max Pool Size=100;""
   },
   "Jwt": {
     "Key": "super-secret-key-12345",
     "Issuer": "ProductOrderAPI",
     "Audience": "ProductOrderAPIClients"
   }
   ```

3. Apply migrations

   ```bash
   dotnet ef database update --project ProductOrderAPI.Infrastructure --startup-project ProductOrderAPI.API
   ```

4. Run the API

   ```bash
   dotnet run --project ProductOrderAPI.API
   ```

   API will be available at `https://localhost:7231/swagger` or `http://localhost:5062/swagger`.

---

API Endpoints

1. Authentication

* `POST /api/auth/register` → Register new user
* `POST /api/auth/login` → Login and get JWT
* `GOT /api/auth` → View Users

2. Products

* `GET /api/products` →   all products
* `GET /api/products/{id}` → View product details
* `POST /api/products` → Add product (Admin only)
* `PUT /api/products/{id}` → Update product
* `PUT /api/products/{id}/add-stock` → Update product by adding more quantity
* `DELETE /api/products/{id}` → Delete product

3. Orders

* `POST /api/orders` → Place an order
* `GET /api/orders/{id}` → View order details
* `GET /api/orders` → List all order 

---

Prepare by

Adeola Oluwafemi

