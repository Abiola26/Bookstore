# 📚 Bookstore API - Production-Ready Implementation

A comprehensive, enterprise-level Online Bookstore REST API built with **.NET 10**, **SQL Server**, and **Clean Architecture** principles.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![License](https://img.shields.io/badge/license-MIT-blue)]()
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)]()

## 🌟 Features

### ✅ Complete Feature Set

- **Book Management**: CRUD operations, search, filtering, pagination
- **Category Management**: Organize books into categories
- **User Authentication**: JWT-based with secure password hashing (BCrypt)
- **Role-Based Authorization**: Admin and User roles
- **Order Management**: Create orders, track status, cancel orders
- **Inventory Management**: Stock management with transaction safety
- **Global Exception Handling**: Centralized error responses
- **API Documentation**: Interactive Swagger/OpenAPI
- **Soft Delete**: Logical deletion with audit trail

### 🏗️ Architecture

- **Clean Architecture**: Domain, Application, Infrastructure, API layers
- **Repository Pattern**: Abstraction over data access
- **Unit of Work**: Transaction management
- **Service Layer**: Business logic orchestration
- **DTOs**: Data Transfer Objects for API contracts
- **Value Objects**: ISBN and Money as domain-driven design values

### 🔒 Security

- **JWT Authentication**: Stateless, token-based authentication
- **Password Security**: BCrypt hashing with adaptive work factor
- **SQL Injection Prevention**: Parameterized queries via EF Core
- **HTTPS**: Enforced in production
- **CORS**: Configurable cross-origin policies
- **Role-Based Access Control**: Fine-grained authorization

### 📊 Data

- **SQL Server**: Robust relational database
- **Entity Framework Core**: Code-First ORM
- **Migrations**: Version-controlled schema changes
- **Soft Delete**: Logical deletion support
- **Optimistic Concurrency**: RowVersion for conflict detection
- **Comprehensive Indexing**: Performance optimization

### 🚀 Performance

- **Pagination**: Efficient data retrieval
- **Eager Loading**: N+1 query prevention
- **Indexing Strategy**: Optimized database queries
- **Connection Pooling**: Efficient resource management
- **Async/Await**: Non-blocking I/O operations
- **Caching Ready**: Designed for Redis integration

---

## 📋 Project Structure

```
Bookstore/
├── Bookstore.Domain/               # Domain entities & value objects
│   ├── Entities/
│   │   ├── BaseEntity.cs          # Base class with audit fields
│   │   ├── Book.cs
│   │   ├── Category.cs
│   │   ├── User.cs
│   │   ├── Order.cs
│   │   └── OrderItem.cs
│   └── ValueObjects/
│       ├── ISBN.cs
│       └── Money.cs
│
├── Bookstore.Application/          # Application services & DTOs
│   ├── DTOs/
│   │   ├── BookDtos.cs
│   │   ├── CategoryDtos.cs
│   │   ├── UserDtos.cs
│   │   └── OrderDtos.cs
│   ├── Services/
│   │   └── IServices.cs
│   ├── Repositories/
│   │   └── IRepositories.cs
│   ├── Validators/
│   │   └── DtoValidators.cs
│   ├── Exceptions/
│   │   └── CustomExceptions.cs
│   └── Common/
│       └── ApiResponse.cs
│
├── Bookstore.Infrastructure/       # EF Core, repositories, services
│   ├── Persistence/
│   │   ├── BookStoreDbContext.cs
│   │   ├── Configurations/
│   │   └── Repositories/
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── BookService.cs
│   │   ├── CategoryService.cs
│   │   └── OrderService.cs
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   └── DependencyInjection.cs
│
├── Bookstore.API/                  # ASP.NET Core API
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── BooksController.cs
│   │   ├── CategoriesController.cs
│   │   └── OrdersController.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── docs/                           # Project documentation
│   ├── api/
│   │   └── POSTMAN_COLLECTION.json # API testing collection
│   ├── history/                    # Historical records & reviews
│   │   ├── CODE_REVIEW_SENIOR_ENGINEER.md
│   │   └── TEST_FAILURES_ANALYSIS_AND_FIXES.md
│   ├── summaries/                  # Feature and logic summaries
│   │   └── SHOPPING_CART_FEATURE_SUMMARY.md
│   ├── BEST_PRACTICES.md           # Implementation best practices
│   ├── DATABASE_MIGRATIONS.md       # Migration guide
│   ├── DEPLOYMENT_CHECKLIST.md      # Deployment guide
│   ├── NUGET_PACKAGES.md           # Required NuGet packages
│   ├── TESTING_GUIDE.md            # Testing strategies
│   └── PROJECT_SPECIFICATION.md     # Core project requirements
└── .editorconfig                   # Code style & cleanup rules
```

---

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server (or SQL Server Express)
- Visual Studio 2026 (or VS Code)

### 1. Clone & Setup

```bash
# Clone repository
git clone https://github.com/your-org/bookstore-api.git
cd bookstore-api

# Restore packages
dotnet restore
```

### 2. Configure Database

Edit `Bookstore.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(local)\\SQLEXPRESS;Database=BookstoreDb;Trusted_Connection=true;"
  },
  "JWT": {
    "Key": "your-secure-key-minimum-32-characters-xxx",
    "Issuer": "BookstoreAPI",
    "Audience": "BookstoreClients"
  }
}
```

### 3. Create Database

**Option A: Using Package Manager Console**
```powershell
Add-Migration InitialCreate
Update-Database
```

**Option B: Using .NET CLI**
```bash
cd Bookstore.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run Application

```bash
cd Bookstore.API
dotnet run

# Application starts at: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger/index.html
```

---

## 📖 API Documentation

### Authentication

```bash
# Register
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123",
  "phoneNumber": "+1234567890"
}

# Login
POST /api/auth/login
{
  "email": "john@example.com",
  "password": "SecurePassword123"
}

# Response
{
  "success": true,
  "data": {
    "userId": "guid",
    "fullName": "John Doe",
    "email": "john@example.com",
    "role": "User",
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "expiresAt": "2025-01-09T12:00:00Z"
  },
  "statusCode": 200
}
```

### Books

```bash
# Get all books (paginated)
GET /api/books?pageNumber=1&pageSize=10

# Get book by ID
GET /api/books/{id}

# Search books
GET /api/books/search/gatsby

# Get books by category
GET /api/books/category/{categoryId}?pageNumber=1&pageSize=10

# Create book (Admin only)
POST /api/books
Authorization: Bearer {admin-token}

# Update book (Admin only)
PUT /api/books/{id}

# Delete book (Admin only)
DELETE /api/books/{id}
```

### Categories

```bash
# Get all categories
GET /api/categories

# Get category by ID
GET /api/categories/{id}

# Create category (Admin)
POST /api/categories

# Update category (Admin)
PUT /api/categories/{id}

# Delete category (Admin)
DELETE /api/categories/{id}
```

### Orders

```bash
# Create order
POST /api/orders
Authorization: Bearer {user-token}

# Get order by ID
GET /api/orders/{id}

# Get user's orders
GET /api/orders/my-orders?pageNumber=1&pageSize=10

# Update order status (Admin)
PUT /api/orders/{id}/status

# Cancel order
DELETE /api/orders/{id}/cancel
```

---

## 🧪 Testing

### Postman Collection

Import `POSTMAN_COLLECTION.json` into Postman for API testing.

**Setup Variables:**
- `base_url`: https://localhost:5001
- `access_token`: Token from login response
- `admin_token`: Admin user's token
- `category_id`, `book_id`, `order_id`: IDs from responses

### Unit Testing

```bash
# Run tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

---

## 📊 Database Schema

### Tables

- **Users**: Registered users with authentication
- **Categories**: Book categories
- **Books**: Book inventory with stock management
- **Orders**: Customer orders
- **OrderItems**: Individual items in orders

### Key Features

- ✅ Referential integrity with foreign keys
- ✅ Unique constraints (ISBN, Email, Category Name)
- ✅ Soft delete with IsDeleted flag
- ✅ Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- ✅ Optimistic concurrency with RowVersion
- ✅ Strategic indexing for performance

---

## 🔒 Security Features

### Authentication
- JWT tokens with 24-hour expiration
- Email and password validation
- Secure password hashing (BCrypt)

### Authorization
- Role-based access control
- Admin-only operations protected
- User resources isolated

### Data Protection
- SQL injection prevention (parameterized queries)
- XSS protection (no inline scripts)
- CORS policy enforcement
- HTTPS enforced

### Audit Trail
- CreatedBy, UpdatedBy tracking
- CreatedAt, UpdatedAt timestamps
- Soft delete for data recovery

---

## 📈 Performance Metrics

- **API Response Time**: < 2 seconds (target)
- **Database Queries**: < 500ms (P95)
- **Pagination**: 10-100 items per page
- **Concurrent Users**: 1000+ (with proper scaling)
- **Order Processing**: < 1 second

---

## 🛠️ Troubleshooting

### Build Errors

**NuGet Restore Failed**
```bash
dotnet nuget locals all --clear
dotnet restore
```

**Missing Assembly References**
```bash
# Verify packages installed
dotnet list package
```

### Runtime Errors

**Connection String Issues**
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure database exists or let EF Core create it

**Migration Failures**
- Check for pending migrations: `dotnet ef migrations list`
- Roll back if needed: `dotnet ef database update <PreviousMigration>`

**Authentication Issues**
- Verify JWT Key is set in appsettings.json
- Check token hasn't expired
- Ensure token is in Authorization header: `Bearer {token}`

---

## 📚 Additional Documentation

- **[Database Migrations](./docs/DATABASE_MIGRATIONS.md)** - Migration guide
- **[Best Practices](./docs/BEST_PRACTICES.md)** - Implementation guidelines
- **[NuGet Packages](./docs/NUGET_PACKAGES.md)** - Required dependencies
- **[Testing Guide](./docs/TESTING_GUIDE.md)** - Testing strategies
- **[Frontend Strategy](./docs/FRONTEND_IMPLEMENTATION.md)** - Modern frontend architecture

---

## 🔄 CI/CD Pipeline

### GitHub Actions Example

```yaml
name: Build & Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.0'
      
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test
```

---

## 📋 Deployment Checklist

- [ ] Database backups configured
- [ ] Connection strings updated (no hardcoded values)
- [ ] JWT keys rotated and secured in Key Vault
- [ ] HTTPS certificate installed
- [ ] Logging configured (structured logging)
- [ ] Monitoring and alerting set up
- [ ] Load balancer configured
- [ ] Cache layer deployed (if using Redis)
- [ ] Firewall rules configured
- [ ] Database replicas set up

---

## 🤝 Contributing

1. Create feature branch: `git checkout -b feature/my-feature`
2. Commit changes: `git commit -am 'Add my feature'`
3. Push to branch: `git push origin feature/my-feature`
4. Submit pull request

### Code Style

- Follow C# naming conventions
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Write unit tests for new features

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🙋 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check existing documentation
- Review troubleshooting guide

---

## 🎉 What's Included

✅ **Backend API**: Full REST API implementation
✅ **Database**: SQL Server with EF Core migrations
✅ **Authentication**: JWT with role-based authorization
✅ **Error Handling**: Global exception middleware
✅ **Logging**: Structured logging throughout
✅ **Documentation**: Comprehensive guides and API docs
✅ **Testing**: Postman collection for API testing
✅ **Security**: BCrypt passwords, SQL injection prevention
✅ **Performance**: Pagination, indexing, lazy loading
✅ **Production Ready**: Best practices implemented

---

## 📞 Contact

**Project Lead**: Abdulmuheez Ogunrinde
**Email**: abdulmuheezabiola@gmail.com 
**GitHub**: Abiola26

---

## 🙏 Acknowledgments

- Microsoft Entity Framework Core team
- ASP.NET Core community
- Security best practices from OWASP

---

**Last Updated**: January 2025
**Version**: 1.0.0  
**Status**: ✅ Production Ready

![Bookstore API Architecture](./docs/architecture.png)

---

## 📌 Quick Links

- [Database Guide](./docs/DATABASE_MIGRATIONS.md)
- [Best Practices](./docs/BEST_PRACTICES.md)
- [Postman Collection](./docs/api/POSTMAN_COLLECTION.json)
- [NuGet Packages](./docs/NUGET_PACKAGES.md)

---

**Happy coding! 🚀**
