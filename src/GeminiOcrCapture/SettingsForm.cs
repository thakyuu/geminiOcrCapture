using GeminiOcrCapture.Core;
using System.Drawing;

namespace GeminiOcrCapture;

/// <summary>
/// アプリケーション設定を変更するためのフォーム
/// </summary>
public class SettingsForm : Form
{
    private readonly ConfigManager _configManager;
    private TextBox _apiKeyTextBox = new TextBox();
    private CheckBox _displayResultCheckBox = new CheckBox();
    private ComboBox _languageComboBox = new ComboBox();
    private Button _saveButton = new Button();
    private Button _cancelButton = new Button();
    private LinkLabel _getApiKeyLink = new LinkLabel();

    /// <summary>
    /// 設定フォームのコンストラクタ
    /// </summary>
    /// <param name="configManager">設定マネージャーのインスタンス</param>
    public SettingsForm(ConfigManager configManager)
    {
        _configManager = configManager;
        InitializeComponents();
        LoadCurrentSettings();
    }

    /// <summary>
    /// フォームのコンポーネントを初期化します
    /// </summary>
    private void InitializeComponents()
    {
        this.Text = "設定";
        this.Width = 450;
        this.Height = 300;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        var titleLabel = new Label
        {
            Text = "Gemini OCR Capture 設定",
            Location = new Point(10, 10),
            Width = 430,
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold)
        };

        // APIキー設定セクション
        var apiKeyGroupBox = new GroupBox
        {
            Text = "API設定",
            Location = new Point(10, 40),
            Width = 410,
            Height = 100
        };

        var apiKeyLabel = new Label
        {
            Text = "Gemini API Key:",
            Location = new Point(10, 25),
            Width = 100
        };
        apiKeyGroupBox.Controls.Add(apiKeyLabel);

        _apiKeyTextBox.Location = new Point(120, 22);
        _apiKeyTextBox.Width = 270;
        _apiKeyTextBox.PasswordChar = '*';
        apiKeyGroupBox.Controls.Add(_apiKeyTextBox);

        _getApiKeyLink.Text = "API Keyを取得する方法";
        _getApiKeyLink.Location = new Point(10, 60);
        _getApiKeyLink.Width = 200;
        _getApiKeyLink.LinkClicked += GetApiKeyLink_LinkClicked;
        apiKeyGroupBox.Controls.Add(_getApiKeyLink);

        // 表示設定セクション
        var displayGroupBox = new GroupBox
        {
            Text = "表示設定",
            Location = new Point(10, 150),
            Width = 410,
            Height = 70
        };

        _displayResultCheckBox.Text = "OCR結果をポップアップ表示する";
        _displayResultCheckBox.Location = new Point(10, 25);
        _displayResultCheckBox.Width = 250;
        _displayResultCheckBox.Checked = true;
        displayGroupBox.Controls.Add(_displayResultCheckBox);

        var languageLabel = new Label
        {
            Text = "言語:",
            Location = new Point(270, 25),
            Width = 50
        };
        displayGroupBox.Controls.Add(languageLabel);

        _languageComboBox.Location = new Point(320, 22);
        _languageComboBox.Width = 80;
        _languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageComboBox.Items.AddRange(new object[] { "ja", "en" });
        displayGroupBox.Controls.Add(_languageComboBox);

        // ボタン
        _saveButton.Text = "保存";
        _saveButton.Location = new Point(260, 230);
        _saveButton.Width = 75;
        _saveButton.DialogResult = DialogResult.OK;
        _saveButton.Click += SaveButton_Click;

        _cancelButton.Text = "キャンセル";
        _cancelButton.Location = new Point(345, 230);
        _cancelButton.Width = 75;
        _cancelButton.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            titleLabel,
            apiKeyGroupBox,
            displayGroupBox,
            _saveButton,
            _cancelButton
        });

        this.AcceptButton = _saveButton;
        this.CancelButton = _cancelButton;
    }

    /// <summary>
    /// 現在の設定を読み込みます
    /// </summary>
    private void LoadCurrentSettings()
    {
        var config = _configManager.CurrentConfig;
        _apiKeyTextBox.Text = config.ApiKey;
        _displayResultCheckBox.Checked = config.DisplayOcrResult;
        _languageComboBox.SelectedItem = config.Language;
    }

    /// <summary>
    /// 保存ボタンがクリックされたときの処理
    /// </summary>
    private void SaveButton_Click(object? sender, EventArgs e)
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
            config.DisplayOcrResult = _displayResultCheckBox.Checked;
            config.Language = _languageComboBox.SelectedItem?.ToString() ?? "ja";
            _configManager.SaveConfig(config);

            MessageBox.Show(
                "設定を保存しました。",
                "保存完了",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"設定の保存に失敗しました。\n{ex.Message}",
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