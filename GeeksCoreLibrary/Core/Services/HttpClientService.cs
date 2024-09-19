using System;
using System.Net.Http;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;

namespace GeeksCoreLibrary.Core.Services;

public class HttpClientService : IHttpClientService, ISingletonService
{
    public HttpClient Client { get; }

    public HttpClientService()
    {
        var socketsHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        Client = new HttpClient(socketsHandler);
    }
}