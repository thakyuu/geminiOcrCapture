using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace GeminiOcrCapture.Core;

public class ToastFailedException : Exception
{
    public ToastFailedException(string message) : base(message) { }
    public ToastFailedException(string message, Exception innerException) : base(message, innerException) { }
}

public class ErrorHandler
{
    private const string ERROR_LOG_FILE = "error.log";
    private readonly string _logFilePath;

    public ErrorHandler(string? basePath = null)
    {
        _logFilePath = Path.Combine(basePath ?? AppContext.BaseDirectory, ERROR_LOG_FILE);
    }

    public void HandleError(Exception ex, string? contextMessage = null)
    {
        var errorMessage = GetUserFriendlyMessage(ex);
        LogError(ex, contextMessage);
        ShowToast(errorMessage);
    }

    public void LogError(Exception ex, string? contextMessage = null)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var message = $"[{timestamp}] {contextMessage ?? "エラーが発生しました"}\n";
            message += $"エラー種別: {ex.GetType().Name}\n";
            message += $"メッセージ: {ex.Message}\n";
            
            // 機密情報が含まれる可能性のあるスタックトレースはデバッグビルドでのみ記録
            #if DEBUG
            message += $"スタックトレース:\n{ex.StackTrace}\n";
            #endif

            message += "----------------------------------------\n";

            // ファイルへの追記
            File.AppendAllText(_logFilePath, message);
        }
        catch (Exception logEx)
        {
            Debug.WriteLine($"ログの書き込みに失敗しました: {logEx.Message}");
        }
    }

    public void ShowToast(string message)
    {
        try
        {
            MessageBox.Show(
                message,
                "Gemini OCR Capture",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"通知の表示に失敗しました: {ex.Message}");
            throw new ToastFailedException("通知の表示に失敗しました。", ex);
        }
    }

    private string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "アプリケーションに必要なアクセス権限がありません。",
            InvalidOperationException when ex.Message.Contains("API") => "Gemini APIとの通信に失敗しました。APIキーを確認してください。",
            HttpRequestException => "ネットワークエラーが発生しました。インターネット接続を確認してください。",
            IOException when ex.Message.Contains("画面のキャプチャ") => "画面のキャプチャに失敗しました。",
            _ => "予期せぬエラーが発生しました。"
        };
    }

    public static class ErrorType
    {
        public const string ApiError = "API_ERROR";
        public const string CaptureError = "CAPTURE_ERROR";
        public const string FileError = "FILE_ERROR";
        public const string NetworkError = "NETWORK_ERROR";
        public const string UnknownError = "UNKNOWN_ERROR";
    }
}