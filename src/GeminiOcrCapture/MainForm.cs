using GeminiOcrCapture.Core;
using System.Drawing;

namespace GeminiOcrCapture;

public partial class MainForm : Form
{
    private readonly ConfigManager _configManager = null!;
    private readonly GeminiService _geminiService = null!;
    private readonly ScreenCapture _screenCapture = null!;
    private readonly ErrorHandler _errorHandler = null!;

    public MainForm()
    {
        InitializeComponent();

        try
        {
            _configManager = new ConfigManager();
            _geminiService = new GeminiService(_configManager);
            _screenCapture = new ScreenCapture();
            _errorHandler = new ErrorHandler();

            InitializeEvents();
            CheckApiKey();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "アプリケーションの初期化に失敗しました。\n" + ex.Message,
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private void InitializeEvents()
    {
        _screenCapture.CaptureCompleted += async (sender, image) =>
        {
            try
            {
                var text = await _geminiService.AnalyzeImageAsync(image);
                Clipboard.SetText(text);
                
                if (_configManager.CurrentConfig.DisplayOcrResult)
                {
                    MessageBox.Show(text, "OCR結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Application.Exit();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex, "OCR処理に失敗しました。");
                Application.Exit();
            }
        };

        _screenCapture.CaptureCancelled += (sender, e) =>
        {
            Application.Exit();
        };
    }

    private void CheckApiKey()
    {
        if (string.IsNullOrEmpty(_configManager.CurrentConfig.ApiKey))
        {
            using var form = new ApiKeyForm(_configManager);
            if (form.ShowDialog() != DialogResult.OK)
            {
                Application.Exit();
                return;
            }
        }

        // キャプチャを開始
        _screenCapture.StartCapture();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // フォームを非表示にする
        this.Visible = false;
        this.ShowInTaskbar = false;
    }
}

public partial class ApiKeyForm : Form
{
    private readonly ConfigManager _configManager;
    private TextBox _apiKeyTextBox = new TextBox();
    private Button _okButton = new Button();
    private Button _cancelButton = new Button();

    public ApiKeyForm(ConfigManager configManager)
    {
        _configManager = configManager;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "API Key設定";
        this.Width = 400;
        this.Height = 150;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        var label = new Label
        {
            Text = "Gemini API Keyを入力してください：",
            Location = new Point(10, 10),
            Width = 380
        };

        _apiKeyTextBox.Location = new Point(10, 30);
        _apiKeyTextBox.Width = 360;
        _apiKeyTextBox.PasswordChar = '*';

        _okButton.Text = "OK";
        _okButton.Location = new Point(210, 70);
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Click += OkButton_Click;

        _cancelButton.Text = "キャンセル";
        _cancelButton.Location = new Point(290, 70);
        _cancelButton.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { label, _apiKeyTextBox, _okButton, _cancelButton });
        this.AcceptButton = _okButton;
        this.CancelButton = _cancelButton;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_apiKeyTextBox.Text))
        {
            MessageBox.Show("APIキーを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.DialogResult = DialogResult.None;
            return;
        }

        try
        {
            var config = _configManager.CurrentConfig;
            config.ApiKey = _apiKeyTextBox.Text;
            _configManager.SaveConfig(config);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"APIキーの保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.DialogResult = DialogResult.None;
        }
    }
}