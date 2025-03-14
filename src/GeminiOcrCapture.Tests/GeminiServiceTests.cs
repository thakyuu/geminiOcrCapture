using System.Drawing;
using FluentAssertions;
using GeminiOcrCapture.Core;
using Moq;

namespace GeminiOcrCapture.Tests;

public class GeminiServiceTests
{
    private readonly Mock<ConfigManager> _mockConfigManager;
    private readonly GeminiService _geminiService;

    public GeminiServiceTests()
    {
        _mockConfigManager = new Mock<ConfigManager>();
        _mockConfigManager.Setup(x => x.CurrentConfig)
            .Returns(new ConfigManager.Config
            {
                ApiKey = "test-api-key",
                Language = "ja"
            });

        _geminiService = new GeminiService(_mockConfigManager.Object);
    }

    [Fact]
    public void Constructor_WhenConfigManagerHasNoApiKey_ShouldThrowException()
    {
        // Arrange
        var mockConfigManager = new Mock<ConfigManager>();
        mockConfigManager.Setup(x => x.CurrentConfig)
            .Returns(new ConfigManager.Config { ApiKey = null });

        // Act & Assert
        var act = () => new GeminiService(mockConfigManager.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("APIキーが設定されていません。");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenImageIsValid_ShouldReturnOcrResult()
    {
        // Arrange
        using var image = new Bitmap(100, 100);

        // Act
        var result = await _geminiService.AnalyzeImageAsync(image);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenImageIsNull_ShouldThrowException()
    {
        // Act
        var act = () => _geminiService.AnalyzeImageAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateApiKey_WhenKeyIsValid_ShouldReturnTrue()
    {
        // Act
        var result = await _geminiService.ValidateApiKeyAsync("test-api-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApiKey_WhenKeyIsInvalid_ShouldReturnFalse()
    {
        // Act
        var result = await _geminiService.ValidateApiKeyAsync("invalid-api-key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKey_WhenKeyIsEmpty_ShouldReturnFalse()
    {
        // Act
        var result = await _geminiService.ValidateApiKeyAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }
}