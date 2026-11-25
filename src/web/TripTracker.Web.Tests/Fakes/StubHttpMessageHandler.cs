using System.Net;
using System.Text;

namespace TripTracker.Web.Tests.Fakes;

/// <summary>Answers every request with a prepared response or exception, and records what was sent.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage> _answer;

    private StubHttpMessageHandler(Func<HttpResponseMessage> answer)
    {
        _answer = answer;
    }

    public HttpMethod? Method { get; private set; }

    public Uri? RequestUri { get; private set; }

    public string? RequestBody { get; private set; }

    public string? Authorization { get; private set; }

    public static StubHttpMessageHandler Responding(
        HttpStatusCode status,
        string body = "",
        string mediaType = "application/json") =>
        new(() => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) });

    public static StubHttpMessageHandler Throwing(Exception exception) => new(() => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Method = request.Method;
        RequestUri = request.RequestUri;
        Authorization = request.Headers.Authorization?.ToString();
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return _answer();
    }
}
