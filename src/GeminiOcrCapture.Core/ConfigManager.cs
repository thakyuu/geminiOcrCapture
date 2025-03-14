using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Diagnostics;

namespace GeminiOcrCapture.Core;

public class ConfigManager
{
    private const string CONFIG_FILE = "config.json";
    private readonly string _configPath;
    private Config _currentConfig;

    public ConfigManager() : this(null) { }

    public ConfigManager(string? basePath)
    {
        _configPath = Path.Combine(basePath ?? AppContext.BaseDirectory, CONFIG_FILE);
        Logger.Log($"設定ファイルのパス: {_configPath}");
        _currentConfig = LoadConfig();
    }

    public class Config
    {
        public string? ApiKey { get; set; }
        public bool DisplayOcrResult { get; set; } = true;
        public bool PlaySoundOnOcrSuccess { get; set; } = true;
        public string? CustomSoundFilePath { get; set; } = null;
        public string Language { get; set; } = "ja";
        public string FullscreenShortcut { get; set; } = "PrintScreen";
    }

    public virtual Config CurrentConfig => _currentConfig;

    public Config LoadConfig()
    {
        Logger.Log($"設定ファイルの読み込みを開始: {_configPath}");
        
        if (!File.Exists(_configPath))
        {
            Logger.Log("設定ファイルが存在しません。新規作成します。");
            _currentConfig = new Config();
            SaveConfig(_currentConfig);
            return _currentConfig;
        }

        try
        {
            Logger.Log("設定ファイルを読み込みます。");
            var json = File.ReadAllText(_configPath);
            
            var config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                try
                {
                    Logger.Log("APIキーの復号化を開始します。");
                    var decryptedKey = DecryptApiKey(config.ApiKey);
                    
                    // 復号化されたキーが空でないことを確認
                    if (!string.IsNullOrEmpty(decryptedKey))
                    {
                        config.ApiKey = decryptedKey;
                        Logger.Log("APIキーの復号化が完了しました。");
                    }
                    else
                    {
                        Logger.Log("復号化されたAPIキーが空です。元のキーを使用します。");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"APIキーの復号化に失敗しました。暗号化されていないキーとして扱います: {ex.Message}");
                    // 復号化に失敗した場合は、元のキーをそのまま使用
                }
            }
            
            // APIキーが設定されているか確認
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                Logger.Log($"APIキーが設定されています: {config.ApiKey.Substring(0, 3)}...{config.ApiKey.Substring(config.ApiKey.Length - 3)}");
            }
            else
            {
                Logger.Log("APIキーが設定されていません。");
            }
            
            // 通知音の設定を確認
            Logger.Log($"通知音の設定: PlaySoundOnOcrSuccess={config.PlaySoundOnOcrSuccess}, CustomSoundFilePath={config.CustomSoundFilePath ?? "未設定"}");
            
            _currentConfig = config;
            return config;
        }
        catch (Exception ex)
        {
            Logger.Log($"設定ファイルの読み込み中にエラーが発生しました: {ex}");
            Logger.Log($"スタックトレース: {ex.StackTrace}");
            throw new InvalidOperationException($"設定ファイルの読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    public void SaveConfig(Config config)
    {
        try
        {
            Logger.Log("設定ファイルの保存を開始します。");
            Logger.Log($"保存する設定: PlaySoundOnOcrSuccess={config.PlaySoundOnOcrSuccess}, CustomSoundFilePath={config.CustomSoundFilePath ?? "未設定"}");
            
            // 保存用の設定オブジェクトを作成（APIキーの暗号化のため）
            var saveConfig = new Config
            {
                DisplayOcrResult = config.DisplayOcrResult,
                PlaySoundOnOcrSuccess = config.PlaySoundOnOcrSuccess,
                CustomSoundFilePath = config.CustomSoundFilePath,
                Language = config.Language,
                FullscreenShortcut = config.FullscreenShortcut
            };
            
            // APIキーが設定されているか確認
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                Logger.Log($"保存するAPIキー: {config.ApiKey.Substring(0, 3)}...{config.ApiKey.Substring(config.ApiKey.Length - 3)}");
                Logger.Log("APIキーの暗号化を開始します。");
                
                var encryptedKey = EncryptApiKey(config.ApiKey);
                
                // 暗号化されたキーが空でないことを確認
                if (!string.IsNullOrEmpty(encryptedKey))
                {
                    saveConfig.ApiKey = encryptedKey;
                    Logger.Log("APIキーの暗号化が完了しました。");
                }
                else
                {
                    saveConfig.ApiKey = config.ApiKey;
                    Logger.Log("暗号化されたAPIキーが空です。暗号化せずに保存します。");
                }
            }
            else
            {
                Logger.Log("APIキーが設定されていません。");
            }

            var json = JsonSerializer.Serialize(saveConfig, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_configPath, json);
            
            // 現在の設定を更新（APIキーは暗号化前の値を保持）
            _currentConfig = config;
            
            Logger.Log("設定ファイルの保存が完了しました。");
            Logger.Log($"保存後の設定: PlaySoundOnOcrSuccess={_currentConfig.PlaySoundOnOcrSuccess}, CustomSoundFilePath={_currentConfig.CustomSoundFilePath ?? "未設定"}");
        }
        catch (Exception ex)
        {
            Logger.Log($"設定ファイルの保存中にエラーが発生しました: {ex.Message}");
            if (ex.InnerException != null)
            {
                Logger.Log($"内部エラー: {ex.InnerException.Message}");
            }
            throw new InvalidOperationException($"設定ファイルの保存に失敗しました: {ex.Message}", ex);
        }
    }

    // APIキーを暗号化するメソッド
    private string EncryptApiKey(string apiKey)
    {
        try
        {
            // 簡易的な暗号化（実際のアプリケーションではより強固な方法を使用すべき）
            byte[] keyBytes = Encoding.UTF8.GetBytes(apiKey);
            byte[] encryptedBytes = new byte[keyBytes.Length];
            
            for (int i = 0; i < keyBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(keyBytes[i] ^ 0x5A); // XOR with 0x5A
            }
            
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            Logger.Log($"APIキーの暗号化に失敗しました: {ex.Message}");
            return string.Empty;
        }
    }

    // APIキーを復号化するメソッド
    private string DecryptApiKey(string encryptedApiKey)
    {
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedApiKey);
            byte[] decryptedBytes = new byte[encryptedBytes.Length];
            
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ 0x5A); // XOR with 0x5A
            }
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            Logger.Log($"APIキーの復号化に失敗しました: {ex.Message}");
            return string.Empty;
        }
    }
}