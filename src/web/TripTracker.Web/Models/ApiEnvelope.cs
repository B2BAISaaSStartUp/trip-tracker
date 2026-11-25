namespace TripTracker.Web.Models;

// TODO: temporary stand-in for SharedKernel's Envelope<T>, which System.Text.Json cannot deserialize: its
// errors are an ErrorList, a custom enumerable with no way to add items (CLAUDE.md, section 7, Envelope<T>
// defect). Remove once SharedKernel is fixed.

/// <summary>The wrapper the API puts around every JSON response.</summary>
public sealed record ApiEnvelope<T>(
    T? Result,
    IReadOnlyList<ApiError>? Errors,
    bool IsError,
    DateTime TimeGenerated);
