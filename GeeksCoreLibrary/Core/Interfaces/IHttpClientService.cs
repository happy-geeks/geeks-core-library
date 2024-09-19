using System.Net.Http;

namespace GeeksCoreLibrary.Core.Interfaces;

public interface IHttpClientService
{
    HttpClient Client { get; }
}