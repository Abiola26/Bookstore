namespace Bookstore.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityName, Guid id) 
        : base($"{entityName} with ID {id} not found.") { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ICollection<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public ValidationException(ICollection<string> errors) 
        : base($"Validation failed with {errors.Count} error(s)")
    {
        Errors = errors;
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized access") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Access forbidden") : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

public class OutOfStockException : Exception
{
    public OutOfStockException(string bookTitle, int requestedQuantity, int availableQuantity)
        : base($"Book '{bookTitle}' has insufficient stock. Requested: {requestedQuantity}, Available: {availableQuantity}")
    {
    }
}

public class InsufficientInventoryException : Exception
{
    public InsufficientInventoryException(string message) : base(message) { }
}
