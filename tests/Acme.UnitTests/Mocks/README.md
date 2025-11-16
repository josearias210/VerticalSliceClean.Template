# Mock Factories

This folder contains reusable mock factory classes that simplify test setup across the unit test suite.

## 📁 Available Factories

### MockUserManagerFactory

Creates properly configured `UserManager<Account>` mocks for testing authentication and account management features.

**Usage:**
```csharp
using Acme.UnitTests.Mocks;

var userManagerMock = MockUserManagerFactory.Create();

// Configure specific behavior
userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
    .ReturnsAsync(testAccount);
```

---

### MockDbContextFactory

Creates configured `IApplicationDbContext` mocks for testing database operations.

**Methods:**

#### `Create()`
Creates a basic DbContext mock with SaveChangesAsync configured.

```csharp
var dbContextMock = MockDbContextFactory.Create();
```

#### `CreateWithTodoItems(mockDbSet)`
Creates a DbContext mock configured with a TodoItems DbSet.

```csharp
var todoList = new List<TodoItem> { /* items */ };
var mockDbSet = todoList.AsQueryable().BuildMockDbSet();
var dbContextMock = MockDbContextFactory.CreateWithTodoItems(mockDbSet.Object);
```

---

### MockServiceFactory

Creates mocks for commonly used application services.

**Available Methods:**

#### `CreateUserIdentityService(userId)`
```csharp
var userId = Guid.NewGuid().ToString();
var userIdentityService = MockServiceFactory.CreateUserIdentityService(userId);
```

#### `CreateTokenService()`
```csharp
var tokenService = MockServiceFactory.CreateTokenService();

// Configure specific behavior
tokenService.Setup(x => x.CreateTokensAsync(It.IsAny<Account>()))
    .ReturnsAsync(("access_token", "refresh_token"));
```

#### `CreateCookieTokenService()`
```csharp
var cookieTokenService = MockServiceFactory.CreateCookieTokenService();
```

#### `CreateEmailService()`
```csharp
var emailService = MockServiceFactory.CreateEmailService();

// Configure specific behavior
emailService.Setup(x => x.SendWelcomeWithPasswordAsync(
    It.IsAny<string>(), 
    It.IsAny<string>(), 
    It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
```

---

## 🎯 Benefits

### 1. **Consistency**
All tests use the same mock creation logic, ensuring consistent test setup.

### 2. **Maintainability**
Changes to mock configuration only need to be made in one place.

### 3. **Simplicity**
Tests remain simple and focused on business logic, not mock setup boilerplate.

### 4. **Flexibility**
Factories provide a starting point that can be further configured for specific test needs.

### 5. **No Inheritance Required**
Tests don't need to inherit from base classes, keeping them independent and easy to understand.

---

## 📝 Example: Complete Test Setup

```csharp
using Acme.UnitTests.Mocks;
using Xunit;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldLoginSuccessfully()
    {
        // Arrange - Use factories for clean setup
        var userManagerMock = MockUserManagerFactory.Create();
        var tokenServiceMock = MockServiceFactory.CreateTokenService();
        var cookieTokenServiceMock = MockServiceFactory.CreateCookieTokenService();
        var loggerMock = new Mock<ILogger<LoginCommandHandler>>();

        // Configure specific behavior
        userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(testAccount);
        
        tokenServiceMock.Setup(x => x.CreateTokensAsync(testAccount))
            .ReturnsAsync(("access", "refresh"));

        var handler = new LoginCommandHandler(
            tokenServiceMock.Object,
            userManagerMock.Object,
            cookieTokenServiceMock.Object,
            loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
    }
}
```

---

## 🔧 Adding New Factories

When adding new services to the application, consider adding factory methods:

1. **Identify frequently mocked services** in test classes
2. **Add factory method** to appropriate factory class (or create new factory)
3. **Update this README** with usage examples
4. **Refactor existing tests** to use the new factory method

### Example: Adding a New Service

```csharp
// In MockServiceFactory.cs
/// <summary>
/// Creates a mock NotificationService.
/// </summary>
public static Mock<INotificationService> CreateNotificationService()
{
    return new Mock<INotificationService>();
}
```

---

## 🚫 When NOT to Use Factories

Factories are great for common scenarios, but you might want to create mocks directly when:

1. **Highly specialized setup** - Unique mock configuration used in only one test
2. **Complex state** - Mock needs extensive configuration that would clutter the factory
3. **Testing the mock itself** - When verifying specific mock behavior

In these cases, create the mock inline in your test method.

---

## 📚 Related Documentation

- [Test Refactoring Summary](../docs/TEST_REFACTORING_SUMMARY.md)
- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
