using GeminiOcrCapture.Core;
using System.Drawing;

namespace GeminiOcrCapture;

/// <summary>
/// APIキーを設定するためのダイアログフォーム
/// </summary>
public partial class ApiKeyForm : Form
{
    private readonly ConfigManager _configManager;
    private TextBox _apiKeyTextBox = new TextBox();
    private Button _okButton = new Button();
    private Button _cancelButton = new Button();
    private LinkLabel _getApiKeyLink = new LinkLabel();

    /// <summary>
    /// APIキー設定フォームのコンストラクタ
    /// </summary>
    /// <param name="configManager">設定マネージャーのインスタンス</param>
    public ApiKeyForm(ConfigManager configManager)
    {
        _configManager = configManager;
        InitializeComponents();
    }

    /// <summary>
    /// フォームのコンポーネントを初期化します
    /// </summary>
    private void InitializeComponents()
    {
        this.Text = "API Key設定";
        this.Width = 400;
        this.Height = 200;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        var titleLabel = new Label
        {
            Text = "Gemini API Keyの設定",
            Location = new Point(10, 10),
            Width = 380,
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold)
        };

        var descriptionLabel = new Label
        {
            Text = "このアプリケーションを使用するには、Google AI StudioからGemini API Keyを取得して入力してください。",
            Location = new Point(10, 40),
            Width = 380,
            Height = 40
        };

        var inputLabel = new Label
        {
            Text = "Gemini API Key:",
            Location = new Point(10, 85),
            Width = 100
        };

        _apiKeyTextBox.Location = new Point(110, 82);
        _apiKeyTextBox.Width = 260;
        _apiKeyTextBox.PasswordChar = '*';

        _getApiKeyLink.Text = "API Keyを取得する方法";
        _getApiKeyLink.Location = new Point(10, 110);
        _getApiKeyLink.Width = 200;
        _getApiKeyLink.LinkClicked += GetApiKeyLink_LinkClicked;

        _okButton.Text = "OK";
        _okButton.Location = new Point(210, 130);
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Click += OkButton_Click;

        _cancelButton.Text = "キャンセル";
        _cancelButton.Location = new Point(290, 130);
        _cancelButton.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { 
            titleLabel, 
            descriptionLabel, 
            inputLabel, 
            _apiKeyTextBox, 
            _getApiKeyLink,
            _okButton, 
            _cancelButton 
        });
        
        this.AcceptButton = _okButton;
        this.CancelButton = _cancelButton;
    }

    /// <summary>
    /// OKボタンがクリックされたときの処理
    /// </summary>
    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_apiKeyTextBox.Text))
        {
            MessageBox.Show(
                "APIキーを入力してください。", 
                "エラー", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
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
            MessageBox.Show(
                $"APIキーの保存に失敗しました。\n{ex.Message}", 
                "エラー", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
            this.DialogResult = DialogResult.None;
        }
    }

    /// <summary>
    /// API Key取得方法のリンクがクリックされたときの処理
    /// </summary>
    private void GetApiKeyLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            // Google AI StudioのAPIキー取得ページを開く
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://aistudio.google.com/app/apikey",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ブラウザを開けませんでした。\n{ex.Message}", 
                "エラー", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }
} 