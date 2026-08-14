namespace LockGo.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message, statusCode: 409)
    {
    }
}
