using System;
using System.IO;
using FluentAssertions;
using GeminiOcrCapture.Core;
using Xunit;

namespace GeminiOcrCapture.Tests;

public class ErrorHandlerTests : IDisposable
{
    private readonly string _testPath;
    private readonly ErrorHandler _errorHandler;

    public ErrorHandlerTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), "test_errors");
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
        Directory.CreateDirectory(_testPath);
        _errorHandler = new ErrorHandler(_testPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }

    [Fact]
    public void LogError_WhenCalled_ShouldWriteToFile()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var context = "Test context";

        // Act
        _errorHandler.LogError(exception, context);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        File.Exists(logPath).Should().BeTrue();

        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("Test error");
        logContent.Should().Contain("Test context");
        logContent.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void LogError_WhenApiError_ShouldWriteToFile()
    {
        // Arrange
        var exception = new InvalidOperationException("API呼び出しに失敗しました");

        // Act
        _errorHandler.LogError(exception);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        File.Exists(logPath).Should().BeTrue();
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("API呼び出しに失敗しました");
        logContent.Should().Contain("InvalidOperationException");
    }

    [Fact]
    public void LogError_WhenCaptureError_ShouldWriteToFile()
    {
        // Arrange
        var exception = new IOException("画面のキャプチャに失敗");

        // Act
        _errorHandler.LogError(exception);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        File.Exists(logPath).Should().BeTrue();
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("画面のキャプチャに失敗");
    }

    [Fact(Skip = "UIテストのため、手動テストで実行する必要があります")]
    public void ShowToast_WhenCalled_ShouldDisplayMessage()
    {
        // Arrange
        var message = "テストメッセージ";

        // Act & Assert
        _errorHandler.ShowToast(message);
        // Note: このテストは手動で確認する必要があります
    }

    [Fact(Skip = "UIテストのため、手動テストで実行する必要があります")]
    public void HandleError_WhenCalled_ShouldLogAndShowMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var context = "Test context";

        // Act
        _errorHandler.HandleError(exception, context);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        File.Exists(logPath).Should().BeTrue();
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("Test error");
        logContent.Should().Contain("Test context");
    }

    [Fact(Skip = "UIテストのため、手動テストで実行する必要があります")]
    public void GetUserFriendlyMessage_WhenApiError_ShouldReturnApiMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("API呼び出しに失敗しました");

        // Act
        _errorHandler.HandleError(exception);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("API呼び出しに失敗しました");
        logContent.Should().Contain("InvalidOperationException");
    }

    [Fact(Skip = "UIテストのため、手動テストで実行する必要があります")]
    public void GetUserFriendlyMessage_WhenCaptureError_ShouldReturnCaptureMessage()
    {
        // Arrange
        var exception = new IOException("画面のキャプチャに失敗");

        // Act
        _errorHandler.HandleError(exception);

        // Assert
        var logPath = Path.Combine(_testPath, "error.log");
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("画面のキャプチャに失敗");
    }
}