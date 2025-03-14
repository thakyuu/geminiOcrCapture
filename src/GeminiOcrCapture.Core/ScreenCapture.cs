using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace GeminiOcrCapture.Core;

public class ScreenCapture : IDisposable
{
    private Rectangle _captureArea;
    private Form[]? _overlays;
    private bool _isCapturing;
    private Point _startPoint;
    private bool _disposed;
    private Screen? _activeScreen;
    private bool _isDragging;

    public event EventHandler<Image>? CaptureCompleted;
    public event EventHandler? CaptureCancelled;

    public void StartCapture()
    {
        _isCapturing = true;
        _isDragging = false;
        ShowOverlays();
    }

    private void ShowOverlays()
    {
        // すべてのスクリーンに対してオーバーレイを作成
        _overlays = Screen.AllScreens.Select(screen =>
        {
            var overlay = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true,
                BackColor = Color.Black,
                Opacity = 0.3,
                Cursor = Cursors.Cross,
                StartPosition = FormStartPosition.Manual, // 手動で位置を設定
                Location = screen.Bounds.Location, // スクリーンの位置に合わせる
                Size = screen.Bounds.Size, // スクリーンのサイズに合わせる
                Owner = null // オーナーウィンドウを設定しない
            };

            // スクリーンの位置とサイズに合わせてオーバーレイを配置
            overlay.Bounds = screen.Bounds;

            overlay.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    CancelCapture();
                }
            };

            overlay.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _activeScreen = screen;
                    _startPoint = e.Location;
                    _startPoint.Offset(screen.Bounds.Location);
                    _captureArea = new Rectangle(_startPoint, Size.Empty);
                    _isDragging = true;
                }
            };

            overlay.MouseMove += (s, e) =>
            {
                if (_isCapturing && _isDragging && e.Button == MouseButtons.Left)
                {
                    var currentPoint = e.Location;
                    currentPoint.Offset(screen.Bounds.Location);

                    int x = Math.Min(_startPoint.X, currentPoint.X);
                    int y = Math.Min(_startPoint.Y, currentPoint.Y);
                    int width = Math.Abs(currentPoint.X - _startPoint.X);
                    int height = Math.Abs(currentPoint.Y - _startPoint.Y);

                    _captureArea = new Rectangle(x, y, width, height);
                    RefreshAllOverlays();
                }
            };

            overlay.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && _isDragging)
                {
                    _isDragging = false;
                    
                    // 最小サイズのチェック
                    if (_captureArea.Width > 5 && _captureArea.Height > 5)
                    {
                        CaptureArea();
                    }
                    else
                    {
                        // 小さすぎる選択はキャンセル
                        _captureArea = Rectangle.Empty;
                        RefreshAllOverlays();
                    }
                }
            };

            overlay.Paint += (s, e) =>
            {
                if (_captureArea.Width > 0 && _captureArea.Height > 0)
                {
                    // スクリーン座標をオーバーレイ座標に変換
                    var localRect = new Rectangle(
                        _captureArea.X - screen.Bounds.X,
                        _captureArea.Y - screen.Bounds.Y,
                        _captureArea.Width,
                        _captureArea.Height
                    );

                    // 選択範囲が現在のスクリーンと交差している場合のみ描画
                    if (screen.Bounds.IntersectsWith(_captureArea))
                    {
                        using var pen = new Pen(Color.LimeGreen, 3);
                        e.Graphics.DrawRectangle(pen, localRect);

                        var sizeText = $"{_captureArea.Width} x {_captureArea.Height}";
                        var font = new Font("Arial", 12);
                        var brush = new SolidBrush(Color.LimeGreen);
                        var textSize = e.Graphics.MeasureString(sizeText, font);
                        var textLocation = new PointF(
                            localRect.X + (localRect.Width - textSize.Width) / 2,
                            localRect.Y + localRect.Height + 5);

                        e.Graphics.DrawString(sizeText, font, brush, textLocation);
                    }
                }
            };

            // フォームのアクティブ化を防ぐ
            overlay.Activated += (s, e) => 
            {
                overlay.BringToFront();
                overlay.Focus();
            };

            return overlay;
        }).ToArray();

        // すべてのオーバーレイを表示
        foreach (var overlay in _overlays)
        {
            overlay.Show();
        }

        // 最前面に表示されることを保証
        foreach (var overlay in _overlays)
        {
            overlay.TopMost = true;
            overlay.BringToFront();
            overlay.Focus();
        }
    }

    private void RefreshAllOverlays()
    {
        if (_overlays != null)
        {
            foreach (var overlay in _overlays)
            {
                overlay.Invalidate();
            }
        }
    }

    private void CaptureArea()
    {
        if (_captureArea.Width <= 0 || _captureArea.Height <= 0)
        {
            CancelCapture();
            return;
        }

        try
        {
            using var bitmap = new Bitmap(_captureArea.Width, _captureArea.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(_captureArea.Location, Point.Empty, _captureArea.Size);

            CloseOverlays();
            CaptureCompleted?.Invoke(this, bitmap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("画面のキャプチャに失敗しました。", ex);
        }
    }

    public void CaptureFullScreen()
    {
        try
        {
            // すべてのスクリーンを含む範囲を計算
            var virtualScreen = new Rectangle(
                SystemInformation.VirtualScreen.Left,
                SystemInformation.VirtualScreen.Top,
                SystemInformation.VirtualScreen.Width,
                SystemInformation.VirtualScreen.Height);

            using var bitmap = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(virtualScreen.Location, Point.Empty, virtualScreen.Size);

            CaptureCompleted?.Invoke(this, bitmap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("フルスクリーンキャプチャに失敗しました。", ex);
        }
    }

    private void CancelCapture()
    {
        CloseOverlays();
        CaptureCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void CloseOverlays()
    {
        if (_overlays != null)
        {
            foreach (var overlay in _overlays)
            {
                if (!overlay.IsDisposed)
                {
                    overlay.Close();
                    overlay.Dispose();
                }
            }
            _overlays = null;
        }
        _isCapturing = false;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CloseOverlays();
                CaptureCompleted = null;
                CaptureCancelled = null;
            }
            _disposed = true;
        }
    }

    ~ScreenCapture()
    {
        Dispose(false);
    }
}