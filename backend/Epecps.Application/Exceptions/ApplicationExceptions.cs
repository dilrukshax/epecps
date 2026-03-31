namespace Epecps.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Dictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public Dictionary<string, string[]>? Errors { get; }
}

/// <summary>
/// Exception thrown for conflicts with current resource state
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
