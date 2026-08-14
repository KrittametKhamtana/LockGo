namespace LockGo.Application.Common.Exceptions;

/// <summary>
/// Signals that a concurrent request claimed the specific compartment this
/// request had picked (optimistic-concurrency loss on Compartment.xmin). It
/// does NOT mean the locker is out of space — another compartment of the same
/// size may still be free, so ReservationService retries rather than
/// surfacing a misleading "no availability" to the caller.
/// Internal signal only; should never reach the API layer.
/// </summary>
public class CompartmentClaimConflictException : Exception
{
    public CompartmentClaimConflictException()
        : base("The selected compartment was claimed by another request.")
    {
    }
}
