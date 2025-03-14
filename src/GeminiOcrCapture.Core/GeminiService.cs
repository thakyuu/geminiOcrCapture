using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Diagnostics;

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
    private readonly IHttpClientWrapper _httpClient;
    private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent";

    public GeminiService(ConfigManager configManager, IHttpClientWrapper? httpClient = null)
    {
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        
        // APIキーが設定されているか確認
        if (string.IsNullOrEmpty(_configManager.CurrentConfig.ApiKey))
        {
            throw new InvalidOperationException("APIキーが設定されていません。");
        }
        
        _httpClient = httpClient ?? new HttpClientWrapper();
        
        // 非同期検証は削除し、実際の使用時に検証するように変更
    }

    public async Task<string> AnalyzeImageAsync(Image image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        // APIキーの検証
        var apiKey = _configManager.CurrentConfig.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.WriteLine("APIキーが設定されていません。");
            throw new InvalidOperationException("APIキーが設定されていません。設定画面からAPIキーを設定してください。");
        }

        try
        {
            Debug.WriteLine("画像の解析を開始します。");
            Debug.WriteLine($"使用するAPIキー: {apiKey.Substring(0, 3)}...{apiKey.Substring(apiKey.Length - 3)}");
            
            // 画像をBase64に変換
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var base64Image = Convert.ToBase64String(ms.ToArray());
            Debug.WriteLine($"画像をBase64に変換しました。サイズ: {base64Image.Length} 文字");

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
                                text = $"この画像に含まれるすべてのテキストを抽出してください。レイアウトは無視して、テキストのみを抽出してください。言語は{_configManager.CurrentConfig.Language}です。"
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
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    topP = 0.1,
                    topK = 16,
                    maxOutputTokens = 2048
                }
            };

            var json = JsonSerializer.Serialize(request);
            Debug.WriteLine("リクエストJSONを作成しました。");
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // APIリクエストの送信
            Debug.WriteLine($"APIリクエストを送信します: {API_BASE_URL}?key=********");
            var response = await _httpClient.PostAsync(
                $"{API_BASE_URL}?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"APIリクエストが失敗しました。ステータスコード: {response.StatusCode}, エラー: {errorContent}");
                
                // エラーメッセージの詳細化
                string errorMessage = $"API呼び出しに失敗しました。ステータスコード: {response.StatusCode}";
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    errorMessage = "APIキーが無効です。設定画面から正しいAPIキーを設定してください。";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // BadRequestの詳細なエラーメッセージを解析
                    try
                    {
                        var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                        if (errorJson.TryGetProperty("error", out var errorObj) && 
                            errorObj.TryGetProperty("message", out var messageObj))
                        {
                            var message = messageObj.GetString();
                            if (message?.Contains("quota") == true)
                            {
                                errorMessage = "APIクォータを超過しました。Google Cloud Consoleで課金設定を確認してください。";
                            }
                            else if (message?.Contains("model not found") == true || message?.Contains("Model gemini-flash is not supported") == true)
                            {
                                errorMessage = "Gemini 2.0 Flashモデルが利用できません。Google Cloud ConsoleでGemini APIが有効化されているか確認してください。";
                            }
                            else
                            {
                                errorMessage = $"リクエストが不正です: {message}";
                            }
                        }
                        else
                        {
                            errorMessage = "リクエストが不正です。画像サイズが大きすぎる可能性があります。";
                        }
                    }
                    catch
                    {
                        errorMessage = "リクエストが不正です。画像サイズが大きすぎる可能性があります。";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    errorMessage = "APIリクエストの制限に達しました。しばらく待ってから再試行してください。";
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    errorMessage = "APIエンドポイントが見つかりません。Gemini APIの有効化とAPIキーの設定を確認してください。";
                }
                
                throw new InvalidOperationException(errorMessage);
            }

            Debug.WriteLine("APIリクエストが成功しました。レスポンスを解析します。");
            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"レスポンス: {responseContent}");
            
            try
            {
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // レスポンスから生成されたテキストを抽出
                string? text = null;
                
                // Gemini 2.0 Flashモデルのレスポンス形式に対応
                if (responseObject.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElement))
                {
                    text = textElement.GetString();
                }
                
                Debug.WriteLine($"抽出されたテキスト: {text}");
                return text ?? "テキストの抽出に失敗しました。";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"レスポンスの解析に失敗しました: {ex.Message}");
                Debug.WriteLine($"レスポンス内容: {responseContent}");
                throw new InvalidOperationException("APIレスポンスの解析に失敗しました。APIの仕様が変更された可能性があります。", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"HTTP通信エラーが発生しました: {ex.Message}");
            throw new InvalidOperationException("ネットワークエラーが発生しました。インターネット接続を確認してください。", ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"画像の解析中にエラーが発生しました: {ex.Message}");
            Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"内部例外: {ex.InnerException.Message}");
                Debug.WriteLine($"内部例外のスタックトレース: {ex.InnerException.StackTrace}");
            }
            throw new InvalidOperationException("画像の解析に失敗しました。" + ex.Message, ex);
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            Debug.WriteLine($"APIキーの検証リクエストを送信します: https://generativelanguage.googleapis.com/v1/models?key=********");
            var response = await _httpClient.GetAsync($"https://generativelanguage.googleapis.com/v1/models?key={apiKey}");
            Debug.WriteLine($"APIキーの検証レスポンス: ステータスコード {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"APIキーの検証に失敗しました: {errorContent}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"APIキーの検証中に例外が発生しました: {ex.Message}");
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