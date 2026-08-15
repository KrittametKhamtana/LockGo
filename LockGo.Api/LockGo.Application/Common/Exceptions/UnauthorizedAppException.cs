namespace LockGo.Application.Common.Exceptions;

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string code, string message) : base(code, message, statusCode: 401)
    {
    }
}
