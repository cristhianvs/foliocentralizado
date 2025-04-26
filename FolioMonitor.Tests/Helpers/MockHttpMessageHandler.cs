// Mocks the handler used by HttpClient to simulate HTTP responses.
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FolioMonitor.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handlerFunc;

    public MockHttpMessageHandler(HttpResponseMessage response)
    {
        _handlerFunc = (request, cancellationToken) => response;
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handlerFunc(request, cancellationToken));
    }
} 