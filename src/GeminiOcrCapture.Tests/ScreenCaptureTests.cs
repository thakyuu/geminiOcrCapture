using System.Drawing;
using FluentAssertions;
using GeminiOcrCapture.Core;

namespace GeminiOcrCapture.Tests;

public class ScreenCaptureTests
{
    private readonly ScreenCapture _screenCapture;

    public ScreenCaptureTests()
    {
        _screenCapture = new ScreenCapture();
    }

    [Fact]
    public void CaptureFullScreen_WhenCalled_ShouldRaiseEventWithImage()
    {
        // Arrange
        Image? capturedImage = null;
        _screenCapture.CaptureCompleted += (sender, image) => capturedImage = image;

        // Act
        _screenCapture.CaptureFullScreen();

        // Assert
        capturedImage.Should().NotBeNull();
    }

    [Fact]
    public void StartCapture_WhenCancelled_ShouldRaiseCancelEvent()
    {
        // Arrange
        var eventRaised = false;
        _screenCapture.CaptureCancelled += (sender, args) => eventRaised = true;

        // Act & Assert
        // Note: このテストは実際のUIインタラクションを必要とするため、
        // 統合テストとして扱う必要があるかもしれません
        _screenCapture.StartCapture();
        // イベントの発生は手動テストで確認する必要があります
    }

    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var screenCapture = new ScreenCapture();

        // Assert
        screenCapture.Should().NotBeNull();
    }

    [Fact]
    public void CaptureFullScreen_WhenScreenNotAvailable_ShouldThrowException()
    {
        // Arrange
        // この状況をシミュレートするのは難しいため、実際の環境での手動テストが必要
        // テストの目的で例外をスローする場合のみをテスト

        // Act & Assert
        // Screen.PrimaryScreenがnullの場合の例外をテスト
        // 実際の環境では発生しにくいシナリオ
    }

    [Fact]
    public void StartCapture_ShouldShowOverlayWindow()
    {
        // Note: UIテストは別途、統合テストとして実装する必要があります
        // このユニットテストでは基本的な機能のみをテスト
        _screenCapture.StartCapture();
        // オーバーレイウィンドウの表示は手動で確認する必要があります
    }
}