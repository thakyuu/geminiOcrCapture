using System.Text.Json;
using FluentAssertions;
using GeminiOcrCapture.Core;

namespace GeminiOcrCapture.Tests;

public class ConfigManagerTests
{
    private readonly string _testConfigPath;

    public ConfigManagerTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), "test_config");
        if (Directory.Exists(_testConfigPath))
        {
            Directory.Delete(_testConfigPath, true);
        }
        Directory.CreateDirectory(_testConfigPath);
    }

    [Fact]
    public void Constructor_WhenCalledWithNullPath_ShouldUseDefaultPath()
    {
        // Act
        var configManager = new ConfigManager();

        // Assert
        configManager.CurrentConfig.Should().NotBeNull();
        configManager.CurrentConfig.Language.Should().Be("ja");
        configManager.CurrentConfig.DisplayOcrResult.Should().BeTrue();
    }

    [Fact]
    public void SaveConfig_WhenCalledWithValidConfig_ShouldSaveToFile()
    {
        // Arrange
        var configManager = new ConfigManager(_testConfigPath);
        var config = new ConfigManager.Config
        {
            ApiKey = "test-api-key",
            DisplayOcrResult = false,
            Language = "en",
            FullscreenShortcut = "F12"
        };

        // Act
        configManager.SaveConfig(config);

        // Assert
        var savedConfigPath = Path.Combine(_testConfigPath, "config.json");
        File.Exists(savedConfigPath).Should().BeTrue();
        
        var jsonContent = File.ReadAllText(savedConfigPath);
        var savedConfig = JsonSerializer.Deserialize<ConfigManager.Config>(jsonContent);
        
        savedConfig.Should().NotBeNull();
        savedConfig!.DisplayOcrResult.Should().BeFalse();
        savedConfig.Language.Should().Be("en");
        savedConfig.FullscreenShortcut.Should().Be("F12");
        // APIキーは暗号化されているため、直接比較はしない
    }

    [Fact]
    public void LoadConfig_WhenFileDoesNotExist_ShouldCreateDefaultConfig()
    {
        // Arrange
        var configManager = new ConfigManager(_testConfigPath);

        // Act
        var config = configManager.LoadConfig();

        // Assert
        config.Should().NotBeNull();
        config.ApiKey.Should().BeNull();
        config.DisplayOcrResult.Should().BeTrue();
        config.Language.Should().Be("ja");
        config.FullscreenShortcut.Should().Be("PrintScreen");
    }

    [Fact]
    public void SaveAndLoadConfig_WhenApiKeyIsSet_ShouldEncryptAndDecryptCorrectly()
    {
        // Arrange
        var configManager = new ConfigManager(_testConfigPath);
        var originalConfig = new ConfigManager.Config
        {
            ApiKey = "test-api-key",
            DisplayOcrResult = true,
            Language = "ja"
        };

        // Act
        configManager.SaveConfig(originalConfig);
        var loadedConfig = configManager.LoadConfig();

        // Assert
        loadedConfig.Should().NotBeNull();
        loadedConfig.ApiKey.Should().Be("test-api-key");
    }

    [Fact]
    public void LoadConfig_WhenFileIsCorrupted_ShouldCreateDefaultConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testConfigPath, "config.json");
        File.WriteAllText(configPath, "This is not valid JSON");
        
        // Act & Assert
        var act = () => new ConfigManager(_testConfigPath);
        
        // ConfigManagerのコンストラクタでLoadConfigを呼び出し、
        // 不正なJSONを検出して例外をスローすることを確認
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*設定ファイルの読み込みに失敗しました*");
    }

    [Fact]
    public void SaveAndLoadConfig_WithCustomSoundSettings_ShouldPersistCorrectly()
    {
        // Arrange
        var configManager = new ConfigManager(_testConfigPath);
        var originalConfig = new ConfigManager.Config
        {
            PlaySoundOnOcrSuccess = true,
            CustomSoundFilePath = @"C:\Sounds\custom.wav"
        };

        // Act
        configManager.SaveConfig(originalConfig);
        var loadedConfig = configManager.LoadConfig();

        // Assert
        loadedConfig.Should().NotBeNull();
        loadedConfig.PlaySoundOnOcrSuccess.Should().BeTrue();
        loadedConfig.CustomSoundFilePath.Should().Be(@"C:\Sounds\custom.wav");
    }
}