namespace LockGo.Application.Common.Exceptions;

/// <summary>
/// Base for exceptions that carry enough information for the API layer to
/// produce the consistent {error:{code,message}} shape without inspecting
/// exception types itself.
/// </summary>
public abstract class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected AppException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}
