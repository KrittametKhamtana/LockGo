namespace LockGo.Application.Common.Exceptions;

public class ValidationAppException : AppException
{
    public ValidationAppException(string message) : base("VALIDATION_ERROR", message, statusCode: 400)
    {
    }
}
