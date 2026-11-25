using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Web.Models;

namespace TripTracker.Web.Services;

/// <summary><see cref="IApiClient"/> over <see cref="HttpClient"/>: sends a request and unwraps the envelope.</summary>
public sealed class ApiClient : IApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates a client. <paramref name="http"/> must have the API address as its base address.</summary>
    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/register", request, ct);

    /// <inheritdoc />
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
        PostAsync<AuthResponse>("api/auth/login", request, ct);

    /// <inheritdoc />
    public Task<ApiResult<PagedList<TripDto>>> GetTripsAsync(ListTripsQuery query, CancellationToken ct = default)
    {
        var newestFirst = query.NewestFirst ? "true" : "false";
        var route = $"api/trips?newestFirst={newestFirst}&page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        return SendAsync<PagedList<TripDto>>(new HttpRequestMessage(HttpMethod.Get, route), ct);
    }

    private Task<ApiResult<T>> PostAsync<T>(string route, object body, CancellationToken ct)
        where T : class =>
        SendAsync<T>(new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) }, ct);

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
        where T : class
    {
        using (request)
        {
            try
            {
                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                return Read<T>(response.StatusCode, body);
            }
            catch (HttpRequestException)
            {
                // In the browser this also covers a request blocked by CORS.
                return ApiResult<T>.Failure([new ApiError(
                    "client.network.unreachable",
                    "Could not reach the server. Check your connection and try again.",
                    ErrorType.FAILURE)]);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // HttpClient reports its own timeout as a cancellation.
                return ApiResult<T>.Failure([new ApiError(
                    "client.network.timeout",
                    "The server took too long to answer. Try again.",
                    ErrorType.FAILURE)]);
            }
        }
    }

    private static ApiResult<T> Read<T>(HttpStatusCode status, string body)
        where T : class
    {
        var envelope = Parse<T>(body);
        if (envelope?.Errors is { Count: > 0 } errors)
        {
            return ApiResult<T>.Failure(errors);
        }

        var isSuccessStatus = (int)status is >= 200 and < 300;
        return isSuccessStatus && envelope?.Result is { } value
            ? ApiResult<T>.Success(value)
            : ApiResult<T>.Failure([FromStatus(status)]);
    }

    /// <summary>Returns <c>null</c> for a body that is empty or is not an envelope.</summary>
    private static ApiEnvelope<T>? Parse<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApiEnvelope<T>>(body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Describes a response that carries no usable envelope. For example, the API answers a missing or
    /// expired token with 401 and an empty body.
    /// </summary>
    private static ApiError FromStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => new(
            "client.request.invalid", "The server rejected the request.", ErrorType.VALIDATION),
        HttpStatusCode.Unauthorized => new(
            "client.auth.required", "You must be signed in to do this.", ErrorType.UNAUTHORIZED),
        HttpStatusCode.Forbidden => new(
            "client.access.forbidden", "You are not allowed to do this.", ErrorType.FORBIDDEN),
        HttpStatusCode.NotFound => new(
            "client.resource.not.found", "The requested item was not found.", ErrorType.NOT_FOUND),
        HttpStatusCode.Conflict => new(
            "client.request.conflict", "The request conflicts with the current data.", ErrorType.CONFLICT),
        HttpStatusCode.TooManyRequests => new(
            "client.requests.too.many", "Too many requests. Try again later.", ErrorType.TOO_MANY_REQUESTS),
        >= HttpStatusCode.InternalServerError => new(
            "client.server.error", "The server could not complete the request.", ErrorType.INTERNAL),
        _ => new(
            "client.response.invalid", "The server sent an unexpected response.", ErrorType.FAILURE),
    };
}
