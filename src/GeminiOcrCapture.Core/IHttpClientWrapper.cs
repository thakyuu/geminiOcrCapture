using System.Net.Http;

namespace GeminiOcrCapture.Core;

public interface IHttpClientWrapper : IDisposable
{
    Task<HttpResponseMessage> GetAsync(string requestUri);
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
}