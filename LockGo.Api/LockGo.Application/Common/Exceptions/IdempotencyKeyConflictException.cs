namespace LockGo.Application.Common.Exceptions;

/// <summary>
/// Signals that two requests raced past the initial idempotency-key check and
/// both tried to insert — the unique DB constraint rejected the second insert.
/// Caught internally by ReservationService, which re-reads and returns the
/// winning reservation. Should never reach the API layer unhandled.
/// </summary>
public class IdempotencyKeyConflictException : Exception
{
    public IdempotencyKeyConflictException(string idempotencyKey)
        : base($"Idempotency key '{idempotencyKey}' was inserted concurrently by another request.")
    {
    }
}
