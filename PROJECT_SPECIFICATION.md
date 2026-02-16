# Online Bookstore API - Project Specification

## 1. Executive Summary

The Online Bookstore API is a backend system designed to manage books, categories, users, and orders. It will provide secure, scalable, and maintainable endpoints for managing bookstore operations and enabling future frontend or mobile integrations.

The system will support inventory management, order processing, and administrative oversight while ensuring data integrity and performance.

## 2. Business Objectives

- Provide a centralized platform for managing books and inventory.
- Enable customers to browse and purchase books.
- Prevent overselling through proper stock validation.
- Allow administrators to manage books, categories, and orders.
- Provide scalable architecture for future enhancements.
- Ensure data security and reliability.

## 3. Scope
### 3.1 In Scope

- Book management (CRUD)
- Category management (CRUD)
- User management
- Order placement
- Stock reduction upon order placement
- Order tracking
- Data validation
- API documentation (Swagger)

### 3.2 Out of Scope (Phase 1)

- Payment gateway integration
- Shipping integration
- Advanced reporting dashboards
- Multi-vendor marketplace features

## 4. Stakeholders

- Business Owner
- System Administrator
- Customers (API consumers)
- Development Team
- QA Team

## 5. Functional Requirements
### 5.1 Book Management

The system shall:

- Allow administrators to create, update, delete, and retrieve books.
- Store book details including:
    - Title
    - Description
    - ISBN (unique)
    - Publisher
    - Price
    - Author
    - Category
    - Language
    - Pages
    - Total Quantity
- Prevent duplicate ISBN entries.
- Allow searching by title.
- Allow filtering by category.
- Support pagination for large datasets.

### 5.2 Category Management

The system shall:

- Allow administrators to create, update, delete categories.
- Prevent duplicate category names.
- Associate books with categories.

### 5.3 User Management

The system shall:

- Store user information (Full Name, Email, Phone).
- Prevent duplicate email addresses.
- Allow users to place orders.

### 5.4 Order Management

The system shall:

- Allow users to create orders.
- Allow adding multiple books to one order.
- Calculate total order amount automatically.
- Reduce book stock when order is successfully placed.
- Prevent order placement if:
    - Book does not exist
    - Book is out of stock
    - Requested quantity exceeds available stock
- Store order status (Pending, Paid, Cancelled, Shipped, Completed).
- Allow administrators to view all orders.
- Allow users to view their own orders.

### 5.5 Inventory Control

The system shall:

- Track total quantity per book.
- Update stock after order placement.
- Prevent negative stock values.
- Keep books visible even if out of stock.

## 6. Non-Functional Requirements
### 6.1 Performance

- API responses should be under 2 seconds under normal load.
- Support concurrent order placement without stock inconsistency.

### 6.2 Security

- Implement authentication (JWT).
- Role-based authorization (Admin, User).
- Protect sensitive endpoints.
- Validate all incoming data.

### 6.3 Scalability

- Support database growth.
- Modular architecture for future microservices migration.
- Clean separation of concerns.

### 6.4 Reliability

- Ensure transactional consistency during order placement.
- Use database constraints for data integrity.

### 6.5 Maintainability

- Follow clean architecture.
- Use DTOs and service layer.
- Use global exception handling.
- Use logging.

## 7. Data Requirements
**Entities Required**

- Book
- Category
- User
- Order
- OrderItem

**Data Constraints**

- ISBN must be unique.
- Email must be unique.
- Price must use decimal (18,2).
- Relationships must enforce referential integrity.

## 8. Assumptions

- SQL Server will be used.
- API-first architecture (frontend separate).
- Identity management may use ASP.NET Identity.
- Currency handling will support future localization.

## 9. Risks

- Stock race conditions during concurrent orders.
- Poor indexing affecting performance.
- Future payment integration complexity.

## 10. Success Criteria

- All CRUD operations function correctly.
- Orders correctly update stock.
- No duplicate ISBN or Email.
- Proper error handling implemented.
- API documentation available via Swagger.
- System passes integration and load testing.
