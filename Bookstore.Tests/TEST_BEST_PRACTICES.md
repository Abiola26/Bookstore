# Enterprise-Level Testing Standards & Best Practices

## Overview
This document outlines the testing standards and best practices implemented in the Bookstore application test suite. These guidelines ensure maintainable, comprehensive, and reliable tests.

## Table of Contents
1. [Test Structure](#test-structure)
2. [Naming Conventions](#naming-conventions)
3. [Mocking Strategies](#mocking-strategies)
4. [Test Organization](#test-organization)
5. [Assertions](#assertions)
6. [Coverage Goals](#coverage-goals)

---

## Test Structure

### AAA Pattern (Arrange-Act-Assert)
All tests follow the AAA pattern for clarity and consistency:

```csharp
[Fact]
public async Task GetBookByIdAsync_WithValidId_ShouldReturnSuccessResponse()
{
    // Arrange - Set up test data and mocks
    var bookId = Guid.NewGuid();
    var book = new BookBuilder().Build();
    
    _bookRepositoryMock.Setup(r => r.GetByIdAsync(bookId, CancellationToken.None))
        .ReturnsAsync(book);

    // Act - Execute the method being tested
    var result = await _service.GetBookByIdAsync(bookId, CancellationToken.None);

    // Assert - Verify the results
    result.IsSuccess.Should().BeTrue();
    result.Data.Should().NotBeNull();
}
```

### Single Responsibility Principle
Each test should verify **one** specific behavior:
- ✅ Test one method call per test
- ✅ Test one condition per test
- ❌ Don't test multiple behaviors in one test

---

## Naming Conventions

### Test Method Names
Use the pattern: `MethodName_Condition_ExpectedResult`

Examples:
```csharp
// Good
GetBookByIdAsync_WithValidId_ShouldReturnSuccessResponse
GetBookByIdAsync_WithInvalidId_ShouldReturnErrorResponse
CreateOrderAsync_WithInsufficientStock_ShouldReturnError
LoginAsync_WithInvalidPassword_ShouldReturnError

// Avoid
TestGetBook
BookTest
TestMethod1
```

### Test Class Names
Use the pattern: `[ClassUnderTest]Tests`

Examples:
```csharp
public class BookServiceTests { }
public class OrderServiceTests { }
public class AuthenticationServiceTests { }
```

---

## Mocking Strategies

### Mock Setup Pattern
```csharp
// Standard setup for async methods with CancellationToken
_repositoryMock.Setup(r => r.GetByIdAsync(id, cancellationToken))
    .ReturnsAsync(expectedValue);

// Setup for methods that throw exceptions
_repositoryMock.Setup(r => r.GetByIdAsync(id, cancellationToken))
    .ThrowsAsync(new Exception("Database error"));

// Setup for void methods
_repositoryMock.Setup(r => r.AddAsync(It.IsAny<Entity>(), cancellationToken))
    .Returns(Task.CompletedTask);
```

### Verification Pattern
```csharp
// Verify method was called exactly once
_repositoryMock.Verify(r => r.AddAsync(It.IsAny<Book>(), cancellationToken), Times.Once);

// Verify method was never called
_repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), cancellationToken), Times.Never);

// Verify method was called with specific arguments
_repositoryMock.Verify(r => r.GetByIdAsync(bookId, cancellationToken), Times.Once);
```

### Mock Initialization Best Practices
```csharp
public class ServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ServiceUnderTest _service;

    public ServiceTests()
    {
        // Create mocks
        _repositoryMock = new Mock<IRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Setup UnitOfWork to return repository mocks
        _unitOfWorkMock.Setup(u => u.SomeRepository).Returns(_repositoryMock.Object);

        // Initialize service with mocked dependencies
        _service = new ServiceUnderTest(_unitOfWorkMock.Object);
    }
}
```

---

## Test Organization

### Test Class Structure
```csharp
public class ServiceTests
{
    // Declare mocks as readonly fields (initialized in constructor)
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ServiceUnderTest _service;

    // Constructor for setup (runs before each test)
    public ServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        
        _unitOfWorkMock.Setup(u => u.Repository).Returns(_repositoryMock.Object);
        _service = new ServiceUnderTest(_unitOfWorkMock.Object);
    }

    // Successful scenario tests (usually first)
    [Fact]
    public async Task MethodAsync_WithValidInput_ShouldSucceed() { }

    // Error/invalid scenario tests
    [Fact]
    public async Task MethodAsync_WithInvalidInput_ShouldReturnError() { }

    // Edge case tests
    [Fact]
    public async Task MethodAsync_WithEdgeCase_ShouldHandleGracefully() { }

    // Exception handling tests
    [Fact]
    public async Task MethodAsync_WhenRepositoryThrowsException_ShouldReturnErrorResponse() { }
}
```

### Test Grouping by Class File
- `ServiceTests.cs` - Core service functionality
- `IntegrationTests.cs` - Cross-service scenarios
- `ValidatorTests.cs` - Data validation rules
- `RepositoryTests.cs` - Data access layer
- `BusinessLogicTests.cs` - Complex business rules

---

## Assertions

### Using FluentAssertions
```csharp
// Use fluent assertions for readability
result.Should().NotBeNull();
result.IsSuccess.Should().BeTrue();
result.Data.Title.Should().Be(expectedTitle);
result.Message.Should().Contain("error");
result.Data.Should().HaveCount(5);

// Collections
collection.Should().NotBeEmpty();
collection.Should().HaveCount(3);
collection.Should().Contain(expectedItem);
collection.Should().AllSatisfy(item => item.Should().NotBeNull());

// String assertions
message.Should().StartWith("Error:");
message.Should().EndWith("try again");
message.Should().Contain("database");
```

### What NOT to Assert
```csharp
// Avoid generic assertions
Assert.NotNull(result);  // ❌ Use: result.Should().NotBeNull();
Assert.True(result.IsSuccess);  // ❌ Use: result.IsSuccess.Should().BeTrue();
Assert.Equal(5, result.Data.Count);  // ❌ Use: result.Data.Should().HaveCount(5);
```

---

## Coverage Goals

### Target Coverage Metrics
- **Overall Code Coverage**: 80-90%
- **Critical Path Coverage**: 100%
- **Service Layer**: 90%+
- **Repository Layer**: 85%+
- **Validators**: 95%+

### Test Distribution
- **Happy Path (Success Cases)**: 60%
- **Error Cases**: 25%
- **Edge Cases**: 15%

### What to Test
✅ **Must Test:**
- All public methods
- Error conditions and exceptions
- Business rule validations
- Boundary conditions
- Integration between services

❌ **Don't Need to Test:**
- Auto-properties
- Framework code (EF Core, ASP.NET)
- Third-party libraries
- Trivial getters/setters

---

## Advanced Testing Patterns

### Theory Tests with Inline Data
```csharp
[Theory]
[InlineData(-10.00)]
[InlineData(0)]
[InlineData(99999999.99)]
public void ValidatePrice_WithVariousAmounts_EnforcesBusinessRules(decimal price)
{
    var isValid = price > 0 && price < 1000000;
    isValid.Should().BeFalse();
}
```

### Theory with Member Data
```csharp
[Theory]
[MemberData(nameof(GetValidBookCreationData))]
public void CreateBook_WithVariousValidData_ShouldSucceed(
    string title, string author, decimal price)
{
    var book = TestDataFactory.CreateValidBook(title, author, price);
    book.Title.Should().Be(title);
}

public static TheoryData<string, string, decimal> GetValidBookCreationData()
{
    return new TheoryData<string, string, decimal>
    {
        { "The C# Player's Guide", "RB Whitaker", 39.99m },
        { "Clean Code", "Robert Martin", 32.00m }
    };
}
```

### Fixture Pattern for Shared Test Data
```csharp
public static class TestDataFactory
{
    public static Book CreateValidBook(
        string title = "Test Book",
        string author = "Test Author",
        decimal price = 29.99m)
    {
        return new Book(
            title,
            "Description",
            new ISBN("978-3-16-148410-0"),
            new Money(price, "USD"),
            author,
            10,
            Guid.NewGuid()
        );
    }
}
```

---

## Running Tests

### Run All Tests
```powershell
dotnet test
```

### Run Specific Test Class
```powershell
dotnet test --filter FullyQualifiedName~Bookstore.Tests.Unit.Infrastructure.Services.BookServiceTests
```

### Run Tests with Coverage
```powershell
dotnet test /p:CollectCoverageMetrics=true
```

---

## Code Review Checklist for Tests

- [ ] Test follows AAA pattern
- [ ] Test name describes what it tests
- [ ] Single assertion focus (usually)
- [ ] No hardcoded magic numbers (use constants or builders)
- [ ] Mocks are properly initialized
- [ ] Assertions use FluentAssertions
- [ ] Test is independent (no dependencies between tests)
- [ ] Test cleans up after itself (if needed)
- [ ] Comments explain complex test logic
- [ ] Test documents expected behavior

---

## Common Pitfalls to Avoid

1. **Testing Implementation Instead of Behavior**
   - ❌ Test internal details
   - ✅ Test observable behavior

2. **Over-Mocking**
   - ❌ Mock everything
   - ✅ Mock only external dependencies

3. **Shared Test State**
   - ❌ Tests that depend on other tests
   - ✅ Each test is independent

4. **Non-Deterministic Tests**
   - ❌ Tests that sometimes pass, sometimes fail
   - ✅ Consistent, reliable tests

5. **Poor Naming**
   - ❌ TestMethod, Test1, DoTest
   - ✅ GetBookByIdAsync_WithValidId_ShouldReturnSuccessResponse

---

## Resources and References

- [xUnit.net Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Guide](https://fluentassertions.com/)
- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
