using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Web.Models;

/// <summary>Errors raised by the client itself, outside the API calls.</summary>
public static class ClientErrors
{
    /// <summary>The API issued a token that this browser already considers expired or cannot read.</summary>
    public static ApiError SessionNotStarted { get; } = new(
        "client.session.not.started",
        "You could not be signed in. Check that your device's date and time are correct.",
        ErrorType.FAILURE);
}
