namespace LockGo.Api.Contracts;

public record ErrorResponse(ErrorDetail Error);

public record ErrorDetail(string Code, string Message);
