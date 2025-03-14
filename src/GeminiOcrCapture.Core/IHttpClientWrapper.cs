using System.Net.Http;

namespace GeminiOcrCapture.Core;

public interface IHttpClientWrapper : IDisposable
{
    Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    Task<HttpResponseMessage> GetAsync(string requestUri);
}

public class HttpClientWrapper : IHttpClientWrapper
{
    private readonly HttpClient _httpClient;

    public HttpClientWrapper()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
    {
        return _httpClient.PostAsync(requestUri, content);
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return _httpClient.GetAsync(requestUri);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}