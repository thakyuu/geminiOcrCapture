using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;

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
        if (!File.Exists(_configPath))
        {
            _currentConfig = new Config();
            SaveConfig(_currentConfig);
            return _currentConfig;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                config.ApiKey = DecryptApiKey(config.ApiKey);
            }
            _currentConfig = config;
            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("設定ファイルの読み込みに失敗しました。", ex);
        }
    }

    public void SaveConfig(Config config)
    {
        try
        {
            if (!string.IsNullOrEmpty(config.ApiKey))
            {
                var encryptedKey = EncryptApiKey(config.ApiKey);
                config = new Config
                {
                    ApiKey = encryptedKey,
                    DisplayOcrResult = config.DisplayOcrResult,
                    Language = config.Language,
                    FullscreenShortcut = config.FullscreenShortcut
                };
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
            _currentConfig = config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("設定ファイルの保存に失敗しました。", ex);
        }
    }

    private string EncryptApiKey(string apiKey)
    {
        try
        {
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

            return Convert.ToBase64String(combinedData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("APIキーの暗号化に失敗しました。", ex);
        }
    }

    private string DecryptApiKey(string encryptedApiKey)
    {
        try
        {
            byte[] combinedData = Convert.FromBase64String(encryptedApiKey);
            var entropy = new byte[16];
            var encryptedBytes = new byte[combinedData.Length - 16];

            Buffer.BlockCopy(combinedData, 0, entropy, 0, entropy.Length);
            Buffer.BlockCopy(combinedData, entropy.Length, encryptedBytes, 0, encryptedBytes.Length);

            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedBytes,
                entropy,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("APIキーの復号化に失敗しました。", ex);
        }
    }
}