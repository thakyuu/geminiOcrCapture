using System;
using System.IO;

namespace GeminiOcrCapture.Core;

public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "debug.log"
    );

    static Logger()
    {
        // ログファイルのディレクトリが存在しない場合は作成
        var directory = Path.GetDirectoryName(LogFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

#if DEBUG
    public static void Log(string message)
    {
        try
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
        catch
        {
            // ログ出力に失敗した場合は無視
        }
    }
#else
    // リリースビルドでは何もしないダミーメソッドを提供
    public static void Log(string message)
    {
        // リリースビルドでは何もしない
    }
#endif
} 