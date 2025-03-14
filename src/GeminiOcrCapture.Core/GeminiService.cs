using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;

namespace GeminiOcrCapture.Core;

public class RequestPart
{
    public string? text { get; set; }
    public InlineData? inline_data { get; set; }
}

public class InlineData
{
    public string mime_type { get; set; } = "image/png";
    public string data { get; set; } = string.Empty;
}

public class GeminiService : IDisposable
{
    private readonly ConfigManager _configManager;
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro-vision:generateContent";

    public GeminiService(ConfigManager configManager)
    {
        _configManager = configManager;
        _httpClient = new HttpClient();
        InitializeClient();
    }

    private void InitializeClient()
    {
        if (string.IsNullOrEmpty(_configManager.CurrentConfig.ApiKey))
        {
            throw new InvalidOperationException("APIキーが設定されていません。");
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> AnalyzeImageAsync(Image image)
    {
        try
        {
            // 画像をBase64に変換
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var base64Image = Convert.ToBase64String(ms.ToArray());

            // リクエストの構築
            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new RequestPart[]
                        {
                            new RequestPart
                            {
                                text = $"この画像から文字を抽出してください。言語は{_configManager.CurrentConfig.Language}です。"
                            },
                            new RequestPart
                            {
                                inline_data = new InlineData
                                {
                                    mime_type = "image/png",
                                    data = base64Image
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // APIリクエストの送信
            var response = await _httpClient.PostAsync(
                $"{API_BASE_URL}?key={_configManager.CurrentConfig.ApiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"API呼び出しに失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // レスポンスから生成されたテキストを抽出
            var text = responseObject
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "テキストの抽出に失敗しました。";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("画像の解析に失敗しました。", ex);
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

public class OcrException : Exception
{
    public OcrException(string message) : base(message) { }
    public OcrException(string message, Exception innerException) : base(message, innerException) { }
}