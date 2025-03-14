using System.Drawing;
using FluentAssertions;
using GeminiOcrCapture.Core;
using Moq;
using System.Net.Http;
using System.Text.Json;

namespace GeminiOcrCapture.Tests;

public class GeminiServiceTests : IDisposable
{
    private readonly Mock<ConfigManager> _mockConfigManager;
    private readonly Mock<IHttpClientWrapper> _mockHttpClient;
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

        _mockHttpClient = new Mock<IHttpClientWrapper>();
        _mockHttpClient.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[]
                                {
                                    new { text = "OCR結果" }
                                }
                            }
                        }
                    }
                }))
            });

        _geminiService = new GeminiService(_mockConfigManager.Object, _mockHttpClient.Object);
    }

    public void Dispose()
    {
        _geminiService.Dispose();
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
        result.Should().Be("OCR結果");
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
    public async Task ValidateApiKeyAsync_WhenKeyIsValid_ShouldReturnTrue()
    {
        // Arrange
        var mockHttpClient = new Mock<IHttpClientWrapper>();
        mockHttpClient.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.OK });

        var service = new GeminiService(_mockConfigManager.Object, mockHttpClient.Object);

        // Act
        var result = await service.ValidateApiKeyAsync("test-api-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenKeyIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var mockHttpClient = new Mock<IHttpClientWrapper>();
        mockHttpClient.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.Unauthorized });

        var service = new GeminiService(_mockConfigManager.Object, mockHttpClient.Object);

        // Act
        var result = await service.ValidateApiKeyAsync("invalid-api-key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenKeyIsEmpty_ShouldReturnFalse()
    {
        // Act
        var result = await _geminiService.ValidateApiKeyAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenApiRequestFails_ShouldThrowOcrException()
    {
        // Arrange
        var mockHttpClient = new Mock<IHttpClientWrapper>();
        mockHttpClient.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        message = "Invalid request"
                    }
                }))
            });

        var service = new GeminiService(_mockConfigManager.Object, mockHttpClient.Object);
        using var image = new Bitmap(100, 100);

        // Act
        var act = () => service.AnalyzeImageAsync(image);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*リクエストが不正です*");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenDifferentLanguageIsSet_ShouldUseCorrectLanguageInRequest()
    {
        // Arrange
        var mockConfigManager = new Mock<ConfigManager>();
        mockConfigManager.Setup(x => x.CurrentConfig)
            .Returns(new ConfigManager.Config
            {
                ApiKey = "test-api-key",
                Language = "en"
            });

        var mockHttpClient = new Mock<IHttpClientWrapper>();
        HttpContent? capturedContent = null;
        
        mockHttpClient.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .Callback<string, HttpContent>((url, content) => capturedContent = content)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[]
                                {
                                    new { text = "OCR result" }
                                }
                            }
                        }
                    }
                }))
            });

        var service = new GeminiService(mockConfigManager.Object, mockHttpClient.Object);
        using var image = new Bitmap(100, 100);

        // Act
        await service.AnalyzeImageAsync(image);

        // Assert
        capturedContent.Should().NotBeNull();
        var contentString = await capturedContent!.ReadAsStringAsync();
        contentString.Should().Contain("en");
        contentString.Should().NotContain("ja");
    }
}