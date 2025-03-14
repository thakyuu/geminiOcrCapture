using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace GeminiOcrCapture.Core;

public class ScreenCapture
{
    private Rectangle _captureArea;
    private Form? _overlay;
    private bool _isCapturing;
    private Point _startPoint;

    public event EventHandler<Image>? CaptureCompleted;
    public event EventHandler? CaptureCancelled;

    public void StartCapture()
    {
        _isCapturing = true;
        ShowOverlay();
    }

    private void ShowOverlay()
    {
        _overlay = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            TopMost = true,
            WindowState = FormWindowState.Maximized,
            BackColor = Color.Black,
            Opacity = 0.3,
            Cursor = Cursors.Cross
        };

        _overlay.KeyPress += (s, e) =>
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                CancelCapture();
            }
        };

        _overlay.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _startPoint = e.Location;
                _captureArea = new Rectangle(_startPoint, Size.Empty);
            }
        };

        _overlay.MouseMove += (s, e) =>
        {
            if (_isCapturing && e.Button == MouseButtons.Left)
            {
                int x = Math.Min(_startPoint.X, e.X);
                int y = Math.Min(_startPoint.Y, e.Y);
                int width = Math.Abs(e.X - _startPoint.X);
                int height = Math.Abs(e.Y - _startPoint.Y);

                _captureArea = new Rectangle(x, y, width, height);
                _overlay.Invalidate();
            }
        };

        _overlay.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                CaptureArea();
            }
        };

        _overlay.Paint += (s, e) =>
        {
            if (_captureArea.Width > 0 && _captureArea.Height > 0)
            {
                using var pen = new Pen(Color.LimeGreen, 3);
                e.Graphics.DrawRectangle(pen, _captureArea);

                var sizeText = $"{_captureArea.Width} x {_captureArea.Height}";
                var font = new Font("Arial", 12);
                var brush = new SolidBrush(Color.LimeGreen);
                var textSize = e.Graphics.MeasureString(sizeText, font);
                var textLocation = new PointF(
                    _captureArea.X + (_captureArea.Width - textSize.Width) / 2,
                    _captureArea.Y + _captureArea.Height + 5);

                e.Graphics.DrawString(sizeText, font, brush, textLocation);
            }
        };

        _overlay.Show();
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

            CloseOverlay();
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
            var bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("プライマリスクリーンが見つかりません。");
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            CaptureCompleted?.Invoke(this, bitmap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("フルスクリーンキャプチャに失敗しました。", ex);
        }
    }

    private void CancelCapture()
    {
        CloseOverlay();
        CaptureCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void CloseOverlay()
    {
        if (_overlay != null && !_overlay.IsDisposed)
        {
            _overlay.Close();
            _overlay.Dispose();
            _overlay = null;
        }
        _isCapturing = false;
    }
}