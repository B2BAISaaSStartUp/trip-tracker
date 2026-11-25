using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Web.Models;
using TripTracker.Web.Services;

namespace TripTracker.Web.Tests.Fakes;

/// <summary>Returns prepared results; trip results are used up in order.</summary>
internal sealed class FakeApiClient : IApiClient
{
    public ApiResult<AuthResponse> AuthResult { get; set; } = ApiResult<AuthResponse>.Success(ServerResponses.Auth());

    public Queue<ApiResult<PagedList<TripDto>>> TripResults { get; } = new();

    public List<LoginRequest> Logins { get; } = [];

    public List<ListTripsQuery> TripQueries { get; } = [];

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
        Task.FromResult(AuthResult);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        Logins.Add(request);
        return Task.FromResult(AuthResult);
    }

    public Task<ApiResult<PagedList<TripDto>>> GetTripsAsync(ListTripsQuery query, CancellationToken ct = default)
    {
        TripQueries.Add(query);
        return Task.FromResult(TripResults.Dequeue());
    }
}
