using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Web.Models;

// TODO: temporary stand-in for SharedKernel's Error, which System.Text.Json cannot deserialize: its only
// constructor is private (CLAUDE.md, section 7, Envelope<T> defect). Remove once SharedKernel is fixed.

/// <summary>One error from the API, or one the client made up when the API sent none.</summary>
/// <param name="Code">Dotted lowercase code, for example <c>auth.email.taken</c>.</param>
/// <param name="Message">English text that can be shown to the user.</param>
/// <param name="Type">Kind of failure. The API sends it as a number.</param>
/// <param name="InvalidField">The request field that failed validation, if the API named one.</param>
public sealed record ApiError(string Code, string Message, ErrorType Type, string? InvalidField = null);
