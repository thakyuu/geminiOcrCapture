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
        Debug.WriteLine($"設定ファイルのパス: {_configPath}");
        _currentConfig = LoadConfig();
    }

    public class Config
    {
        public string? ApiKey { get; set; }
        public bool DisplayOcrResult { get; set; } = true;
        public string Language { get; set; } = "ja";
        public string FullscreenShortcut { get; set; } = "PrintScreen";
    }

    public virtual Config CurrentConfig => _currentConfig;

    public Config LoadConfig()
    {
        Debug.WriteLine($"設定ファイルの読み込みを開始: {_configPath}");
        
        if (!File.Exists(_configPath))
        {
            Debug.WriteLine("設定ファイルが存在しません。新規作成します。");
            _currentConfig = new Config();
            SaveConfig(_currentConfig);
            return _currentConfig;
        }

        try
        {
            Debug.WriteLine("設定ファイルを読み込みます。");
            var json = File.ReadAllText(_configPath);
            Debug.WriteLine($"読み込んだJSON: {json}");
            
            var config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                try
                {
                    Debug.WriteLine("APIキーの復号化を開始します。");
                    var decryptedKey = DecryptApiKey(config.ApiKey);
                    
                    // 復号化されたキーが空でないことを確認
                    if (!string.IsNullOrEmpty(decryptedKey))
                    {
                        config.ApiKey = decryptedKey;
                        Debug.WriteLine("APIキーの復号化が完了しました。");
                    }
                    else
                    {
                        Debug.WriteLine("復号化されたAPIキーが空です。元のキーを使用します。");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"APIキーの復号化に失敗しました。暗号化されていないキーとして扱います: {ex.Message}");
                    // 復号化に失敗した場合は、元のキーをそのまま使用
                }
            }
            
            // APIキーが設定されているか確認
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.WriteLine($"APIキーが設定されています: {config.ApiKey.Substring(0, 3)}...{config.ApiKey.Substring(config.ApiKey.Length - 3)}");
            }
            else
            {
                Debug.WriteLine("APIキーが設定されていません。");
            }
            
            _currentConfig = config;
            return config;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"設定ファイルの読み込み中にエラーが発生しました: {ex}");
            Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            throw new InvalidOperationException($"設定ファイルの読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    public void SaveConfig(Config config)
    {
        try
        {
            Debug.WriteLine("設定ファイルの保存を開始します。");
            
            // APIキーが設定されているか確認
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.WriteLine($"保存するAPIキー: {config.ApiKey.Substring(0, 3)}...{config.ApiKey.Substring(config.ApiKey.Length - 3)}");
                Debug.WriteLine("APIキーの暗号化を開始します。");
                
                var encryptedKey = EncryptApiKey(config.ApiKey);
                
                // 暗号化されたキーが空でないことを確認
                if (!string.IsNullOrEmpty(encryptedKey))
                {
                    config = new Config
                    {
                        ApiKey = encryptedKey,
                        DisplayOcrResult = config.DisplayOcrResult,
                        Language = config.Language,
                        FullscreenShortcut = config.FullscreenShortcut
                    };
                    Debug.WriteLine("APIキーの暗号化が完了しました。");
                }
                else
                {
                    Debug.WriteLine("暗号化されたAPIキーが空です。暗号化せずに保存します。");
                }
            }
            else
            {
                Debug.WriteLine("APIキーが設定されていません。");
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Debug.WriteLine($"保存するJSON: {json}");
            
            File.WriteAllText(_configPath, json);
            _currentConfig = config;
            Debug.WriteLine("設定ファイルの保存が完了しました。");
            
            // 保存後に再読み込みして確認
            var reloadedConfig = LoadConfig();
            if (string.IsNullOrEmpty(reloadedConfig.ApiKey) && !string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.WriteLine("警告: 保存後の再読み込みでAPIキーが空になっています。");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"設定ファイルの保存中にエラーが発生しました: {ex}");
            Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            throw new InvalidOperationException($"設定ファイルの保存に失敗しました: {ex.Message}", ex);
        }
    }

    private string EncryptApiKey(string apiKey)
    {
        try
        {
            Debug.WriteLine("APIキーの暗号化処理を開始します。");
            var entropy = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(entropy);
            }

            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(apiKey),
                entropy,
                DataProtectionScope.CurrentUser);

            var combinedData = new byte[entropy.Length + encryptedData.Length];
            Buffer.BlockCopy(entropy, 0, combinedData, 0, entropy.Length);
            Buffer.BlockCopy(encryptedData, 0, combinedData, entropy.Length, encryptedData.Length);

            var result = Convert.ToBase64String(combinedData);
            Debug.WriteLine("APIキーの暗号化処理が完了しました。");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"APIキーの暗号化中にエラーが発生しました: {ex}");
            Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            throw new InvalidOperationException($"APIキーの暗号化に失敗しました: {ex.Message}", ex);
        }
    }

    private string DecryptApiKey(string encryptedApiKey)
    {
        try
        {
            Debug.WriteLine("APIキーの復号化処理を開始します。");
            
            // Base64形式かどうかを確認
            if (!IsBase64String(encryptedApiKey))
            {
                Debug.WriteLine("APIキーはBase64形式ではありません。暗号化されていないキーとして扱います。");
                return encryptedApiKey;
            }
            
            byte[] combinedData = Convert.FromBase64String(encryptedApiKey);
            var entropy = new byte[16];
            var encryptedBytes = new byte[combinedData.Length - 16];

            Buffer.BlockCopy(combinedData, 0, entropy, 0, entropy.Length);
            Buffer.BlockCopy(combinedData, entropy.Length, encryptedBytes, 0, encryptedBytes.Length);

            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedBytes,
                entropy,
                DataProtectionScope.CurrentUser);

            var result = Encoding.UTF8.GetString(decryptedData);
            Debug.WriteLine("APIキーの復号化処理が完了しました。");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"APIキーの復号化中にエラーが発生しました: {ex}");
            Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            // 復号化に失敗した場合は、元のキーをそのまま返す
            return encryptedApiKey;
        }
    }
    
    // Base64形式かどうかを確認するヘルパーメソッド
    private bool IsBase64String(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
            
        s = s.Trim();
        return s.Length % 4 == 0 && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
    }
}