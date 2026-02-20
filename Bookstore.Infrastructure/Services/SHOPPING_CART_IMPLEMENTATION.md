# Shopping Cart Persistence Implementation

## Overview

This document describes the Shopping Cart persistence feature implementation for the Bookstore application. The Shopping Cart service provides a dedicated, persistent storage mechanism for managing items before checkout.

## Architecture

The Shopping Cart feature follows the clean architecture pattern used throughout the Bookstore application:

### Domain Layer (`Bookstore.Domain`)

#### Entities

**`ShoppingCart.cs`**
- Core aggregate root for shopping cart functionality
- One cart per user (enforced by unique constraint on `UserId`)
- Manages a collection of `ShoppingCartItem` entities
- Provides business logic for:
  - Adding items with automatic quantity updates for duplicates
  - Removing items
  - Updating item quantities with stock validation
  - Clearing the entire cart
  - Calculating total price with auto-recalculation
- Tracks `LastModified` timestamp for cart updates
- Inherits from `BaseEntity` (includes soft-delete support)

**`ShoppingCartItem.cs`**
- Value object representing individual items in a cart
- Links a book to the cart with:
  - Quantity validation (must be > 0)
  - Unit price snapshot (stored at time of add)
  - Subtotal calculation
- Inherits from `BaseEntity` (includes soft-delete support)

### Application Layer (`Bookstore.Application`)

#### DTOs

**`ShoppingCartDtos.cs`** contains:

- **`AddToCartDto`** - Input DTO for adding items
  - `BookId`: Target book identifier
  - `Quantity`: Number of items to add

- **`UpdateCartItemDto`** - Input DTO for updating quantities
  - `Quantity`: New quantity for the item

- **`ShoppingCartItemResponseDto`** - Item representation in responses
  - Complete item details with calculated subtotal
  - Book information (title, ISBN)
  - Unit price and currency

- **`ShoppingCartResponseDto`** - Complete cart representation
  - Cart metadata (ID, user ID, timestamps)
  - Item count and empty status
  - Total price with currency
  - Collection of all items

- **`ClearCartDto`** - Confirmation DTO for cart clearing

#### Service Interface

**`IShoppingCartService`** (in `IServices.cs`)

```csharp
public interface IShoppingCartService
{
    Task<ApiResponse<ShoppingCartResponseDto>> GetUserCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShoppingCartResponseDto>> AddToCartAsync(Guid userId, AddToCartDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShoppingCartResponseDto>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShoppingCartResponseDto>> RemoveFromCartAsync(Guid userId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task<ApiResponse<ShoppingCartResponseDto>> ClearCartAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

#### Repository Interface

**`IShoppingCartRepository`** (in `IRepositories.cs`)

```csharp
public interface IShoppingCartRepository : IGenericRepository<ShoppingCart>
{
    Task<ShoppingCart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetWithItemsAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<ShoppingCart?> GetUserCartWithItemsAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

### Infrastructure Layer (`Bookstore.Infrastructure`)

#### Repository Implementation

**`ShoppingCartRepository.cs`**
- Implements `IShoppingCartRepository`
- Extends `GenericRepository<ShoppingCart>`
- Features:
  - Automatically filters soft-deleted carts
  - Eager loads items with related book entities
  - Optimized queries for cart retrieval

#### Service Implementation

**`ShoppingCartService.cs`**
- Implements `IShoppingCartService`
- Provides comprehensive cart operations:
  - **GetUserCartAsync**: Retrieves or creates cart for user
  - **AddToCartAsync**: Adds items with stock validation
  - **UpdateCartItemAsync**: Updates quantities with stock checks
  - **RemoveFromCartAsync**: Removes specific items
  - **ClearCartAsync**: Empties entire cart
- Business logic:
  - Auto-creation of cart on first access
  - Stock availability validation
  - Duplicate item detection and quantity merging
  - Prevents adding deleted books
  - Automatic total price calculation

#### Database Configuration

**`ShoppingCartConfiguration.cs`**

Fluent API configuration for:
- **ShoppingCart**
  - Money value object configuration for `TotalPrice`
  - Foreign key relationship to `User` (cascade delete)
  - One-to-many relationship with `ShoppingCartItem`
  - Unique constraint on `UserId` (with soft-delete filter)

- **ShoppingCartItem**
  - Money value object configuration for `UnitPrice`
  - Foreign key relationships (cart cascade, book restrict)
  - Unique constraint on (ShoppingCartId, BookId) pair

#### Dependency Injection

**`DependencyInjection.cs`** registration:
```csharp
services.AddScoped<IShoppingCartService, ShoppingCartService>();
```

Database sets added to `BookStoreDbContext`:
```csharp
public DbSet<ShoppingCart> ShoppingCarts { get; set; } = null!;
public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; } = null!;
```

### API Layer (`Bookstore.API`)

#### ShoppingCartController

**`ShoppingCartController.cs`**

RESTful endpoints for cart management:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/shoppingcart` | Get current user's cart |
| POST | `/api/shoppingcart/items` | Add item to cart |
| PUT | `/api/shoppingcart/items/{cartItemId}` | Update item quantity |
| DELETE | `/api/shoppingcart/items/{cartItemId}` | Remove item from cart |
| DELETE | `/api/shoppingcart` | Clear entire cart |

- Requires JWT authentication (`[Authorize]`)
- Extracts user ID from JWT claims
- Returns standardized `ApiResponse<T>` format
- Comprehensive error handling and logging

## Usage Examples

### 1. Get User's Shopping Cart
```http
GET /api/shoppingcart
Authorization: Bearer <token>
```

Response:
```json
{
  "success": true,
  "data": {
    "id": "cart-id",
    "userId": "user-id",
    "totalPrice": 99.97,
    "currency": "USD",
    "itemCount": 2,
    "isEmpty": false,
    "items": [
      {
        "id": "item-id-1",
        "bookId": "book-id-1",
        "bookTitle": "The Great Gatsby",
        "isbn": "978-0-7432-7356-5",
        "quantity": 1,
        "unitPrice": 15.99,
        "currency": "USD",
        "subTotal": 15.99
      },
      {
        "id": "item-id-2",
        "bookId": "book-id-2",
        "bookTitle": "1984",
        "isbn": "978-0-545-01022-1",
        "quantity": 5,
        "unitPrice": 16.79,
        "currency": "USD",
        "subTotal": 83.95
      }
    ],
    "lastModified": "2025-01-20T10:30:00Z"
  },
  "statusCode": 200
}
```

### 2. Add Item to Cart
```http
POST /api/shoppingcart/items
Authorization: Bearer <token>
Content-Type: application/json

{
  "bookId": "book-id",
  "quantity": 2
}
```

**Validations:**
- Quantity must be > 0
- Book must exist and not be deleted
- Stock must be sufficient

### 3. Update Item Quantity
```http
PUT /api/shoppingcart/items/{cartItemId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "quantity": 3
}
```

### 4. Remove Item from Cart
```http
DELETE /api/shoppingcart/items/{cartItemId}
Authorization: Bearer <token>
```

### 5. Clear Entire Cart
```http
DELETE /api/shoppingcart
Authorization: Bearer <token>
```

## Key Features

### 1. **Persistence**
- Full database persistence using Entity Framework Core
- Soft-delete support inherited from `BaseEntity`
- Audit fields (`CreatedAt`, `UpdatedAt`)

### 2. **Data Integrity**
- Unique constraint: one cart per user
- Unique constraint: one item per (cart, book) pair
- Automatic duplicate detection and quantity updates
- Stock availability validation

### 3. **Performance**
- Efficient eager loading of items and related books
- Optimized queries with LINQ to SQL filtering
- Soft-delete filters applied at query level

### 4. **Error Handling**
- Comprehensive validation at service layer
- Descriptive error messages
- HTTP status codes:
  - 400: Invalid input or insufficient stock
  - 404: Cart or item not found
  - 410: Book no longer available
  - 500: Internal server error

### 5. **Security**
- JWT authentication required
- User can only access their own cart
- User ID extracted from claims

## Database Schema

### ShoppingCarts Table
```sql
CREATE TABLE ShoppingCarts (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL UNIQUE (WHERE IsDeleted = 0),
    TotalPrice_Amount DECIMAL(19,2) NOT NULL,
    TotalPrice_Currency VARCHAR(3) NOT NULL,
    LastModified TIMESTAMPTZ NOT NULL,
    IsDeleted BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### ShoppingCartItems Table
```sql
CREATE TABLE ShoppingCartItems (
    Id UUID PRIMARY KEY,
    ShoppingCartId UUID NOT NULL,
    BookId UUID NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice_Amount DECIMAL(19,2) NOT NULL,
    UnitPrice_Currency VARCHAR(3) NOT NULL,
    IsDeleted BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMPTZ NOT NULL,
    UpdatedAt TIMESTAMPTZ NOT NULL,
    UNIQUE (ShoppingCartId, BookId) WHERE IsDeleted = 0,
    FOREIGN KEY (ShoppingCartId) REFERENCES ShoppingCarts(Id) ON DELETE CASCADE,
    FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE RESTRICT
);
```

## Integration with Existing Systems

### Order Creation from Cart
The shopping cart provides persistence for items before checkout. To create an order:
1. User adds items to cart via `/api/shoppingcart/items`
2. User reviews cart via `GET /api/shoppingcart`
3. User creates order via `/api/orders` (order service reads from cart or receives item list)
4. Cart is cleared after successful order creation

### Inventory Management
- Shopping cart respects current book inventory
- Stock validation prevents over-ordering
- Uses existing `Book.TotalQuantity` property

### User Management
- Cart auto-created on first access
- Cart deleted if user is deleted (cascade)
- Each user has exactly one active cart

## Future Enhancements

1. **Cart Expiration**: Add TTL for abandoned carts
2. **Price Updates**: Implement price change notifications for cart items
3. **Wishlist Integration**: Move items between cart and wishlist
4. **Bulk Operations**: Batch add/remove/update items
5. **Cart Sharing**: Share carts with other users (collaborative shopping)
6. **Analytics**: Track popular items added to carts

## Testing

The implementation passes all existing tests plus maintains compatibility with:
- Order creation workflow
- User management
- Book inventory
- Authentication and authorization

All 101 unit and integration tests pass successfully.

## Migration Notes

When deploying:
1. A database migration will create `ShoppingCarts` and `ShoppingCartItems` tables
2. No data transformation needed (new feature)
3. Foreign key constraints ensure referential integrity
4. Soft-delete filters prevent accessing deleted items

## Dependencies

- Entity Framework Core 10.0+
- .NET 10
- PostgreSQL
- FluentAssertions (for testing)
- Xunit (for testing)

## Notes

- The shopping cart implements the **Aggregate Root** pattern with `ShoppingCart` as the aggregate
- **Event Sourcing** can be added later if audit trail is needed
- **Caching** can be added for frequent cart access patterns
- The service follows **SOLID principles** with clear separation of concerns
