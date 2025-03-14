using GeminiOcrCapture.Core;
using System.Drawing;
using System.Linq;
using System.Diagnostics;

namespace GeminiOcrCapture;

/// <summary>
/// アプリケーション設定を変更するためのフォーム
/// </summary>
public class SettingsForm : Form
{
    private readonly ConfigManager _configManager;
    private readonly MainForm _mainForm;
    private TextBox _apiKeyTextBox = new TextBox();
    private CheckBox _displayResultCheckBox = new CheckBox();
    private CheckBox _playSoundCheckBox = new CheckBox();
    private TextBox _soundFilePathTextBox = new TextBox();
    private Button _browseSoundFileButton = new Button();
    private ComboBox _languageComboBox = new ComboBox();
    private Button _saveButton = new Button();
    private Button _cancelButton = new Button();
    private LinkLabel _getApiKeyLink = new LinkLabel();
    private bool _isInitialSetup = false;

    /// <summary>
    /// 設定フォームのコンストラクタ
    /// </summary>
    /// <param name="configManager">設定マネージャーのインスタンス</param>
    /// <param name="mainForm">メインフォームのインスタンス</param>
    public SettingsForm(ConfigManager configManager, MainForm mainForm)
    {
        _configManager = configManager;
        _mainForm = mainForm;
        InitializeComponents();
        LoadCurrentSettings();
        
        // APIキーが未設定の場合は初期設定モードとする
        _isInitialSetup = string.IsNullOrWhiteSpace(_configManager.CurrentConfig.ApiKey);
    }

    /// <summary>
    /// フォームのコンポーネントを初期化します
    /// </summary>
    private void InitializeComponents()
    {
        this.Text = "設定";
        this.Width = 450;
        this.Height = 350;
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
            Height = 150
        };

        _displayResultCheckBox.Text = "OCR結果をポップアップ表示する";
        _displayResultCheckBox.Location = new Point(10, 25);
        _displayResultCheckBox.Width = 250;
        _displayResultCheckBox.Checked = true;
        displayGroupBox.Controls.Add(_displayResultCheckBox);

        _playSoundCheckBox.Text = "OCR成功時に通知音を鳴らす";
        _playSoundCheckBox.Location = new Point(10, 50);
        _playSoundCheckBox.Width = 250;
        _playSoundCheckBox.Checked = true;
        _playSoundCheckBox.CheckedChanged += PlaySoundCheckBox_CheckedChanged;
        displayGroupBox.Controls.Add(_playSoundCheckBox);

        var soundFileLabel = new Label
        {
            Text = "通知音ファイル:",
            Location = new Point(30, 80),
            Width = 100
        };
        displayGroupBox.Controls.Add(soundFileLabel);

        _soundFilePathTextBox.Location = new Point(130, 77);
        _soundFilePathTextBox.Width = 200;
        displayGroupBox.Controls.Add(_soundFilePathTextBox);

        _browseSoundFileButton.Text = "参照...";
        _browseSoundFileButton.Location = new Point(335, 76);
        _browseSoundFileButton.Width = 60;
        _browseSoundFileButton.Click += BrowseSoundFileButton_Click;
        displayGroupBox.Controls.Add(_browseSoundFileButton);

        var soundFileHintLabel = new Label
        {
            Text = "※空白の場合は標準の通知音が使用されます",
            Location = new Point(30, 105),
            Width = 350,
            Font = new Font(this.Font.FontFamily, 8)
        };
        displayGroupBox.Controls.Add(soundFileHintLabel);

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
        _saveButton.Location = new Point(260, 310);
        _saveButton.Width = 75;
        _saveButton.DialogResult = DialogResult.OK;
        _saveButton.Click += SaveButton_Click;

        _cancelButton.Text = "キャンセル";
        _cancelButton.Location = new Point(345, 310);
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
        
        // 初期状態の設定
        UpdateSoundFileControlsState();
    }

    /// <summary>
    /// 通知音チェックボックスの状態が変更されたときの処理
    /// </summary>
    private void PlaySoundCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateSoundFileControlsState();
    }
    
    /// <summary>
    /// 通知音ファイル関連コントロールの状態を更新します
    /// </summary>
    private void UpdateSoundFileControlsState()
    {
        bool enabled = _playSoundCheckBox.Checked;
        _soundFilePathTextBox.Enabled = enabled;
        _browseSoundFileButton.Enabled = enabled;
    }
    
    /// <summary>
    /// 通知音ファイル参照ボタンがクリックされたときの処理
    /// </summary>
    private void BrowseSoundFileButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "通知音ファイルを選択",
            Filter = "音声ファイル (*.wav)|*.wav|すべてのファイル (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true
        };
        
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _soundFilePathTextBox.Text = dialog.FileName;
        }
    }

    /// <summary>
    /// 現在の設定を読み込みます
    /// </summary>
    private void LoadCurrentSettings()
    {
        var config = _configManager.CurrentConfig;
        _apiKeyTextBox.Text = config.ApiKey;
        _displayResultCheckBox.Checked = config.DisplayOcrResult;
        _playSoundCheckBox.Checked = config.PlaySoundOnOcrSuccess;
        _soundFilePathTextBox.Text = config.CustomSoundFilePath ?? string.Empty;
        _languageComboBox.SelectedItem = config.Language;
        
        // コントロールの状態を更新
        UpdateSoundFileControlsState();
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
            Logger.Log("設定の保存を開始します...");
            var config = _configManager.CurrentConfig;
            bool isNewApiKey = string.IsNullOrWhiteSpace(config.ApiKey) && !string.IsNullOrWhiteSpace(_apiKeyTextBox.Text);
            
            // 通知音ファイルパスの設定
            string? oldSoundPath = config.CustomSoundFilePath;
            string? newSoundPath = string.IsNullOrWhiteSpace(_soundFilePathTextBox.Text) ? null : _soundFilePathTextBox.Text;
            config.CustomSoundFilePath = newSoundPath;
            Logger.Log($"通知音ファイルパスを変更: {oldSoundPath ?? "未設定"} → {newSoundPath ?? "未設定"}");
            
            config.ApiKey = _apiKeyTextBox.Text;
            config.Language = _languageComboBox.SelectedItem?.ToString() ?? "ja";
            
            // 初期設定時はDisplayOcrResultの設定をダイアログで確認する
            if (_isInitialSetup || isNewApiKey)
            {
                if (ShowNotificationSettingDialog())
                {
                    config.DisplayOcrResult = true;
                }
                else
                {
                    config.DisplayOcrResult = false;
                }
                
                // 通知音設定も確認する
                if (ShowSoundSettingDialog())
                {
                    config.PlaySoundOnOcrSuccess = true;
                }
                else
                {
                    config.PlaySoundOnOcrSuccess = false;
                }
            }
            else
            {
                // 通常の設定変更時はチェックボックスの値を使用
                config.DisplayOcrResult = _displayResultCheckBox.Checked;
                bool oldPlaySound = config.PlaySoundOnOcrSuccess;
                config.PlaySoundOnOcrSuccess = _playSoundCheckBox.Checked;
                Logger.Log($"通知音の有効/無効を変更: {oldPlaySound} → {config.PlaySoundOnOcrSuccess}");
            }
            
            Logger.Log("ConfigManagerに設定を保存します...");
            _configManager.SaveConfig(config);
            Logger.Log("設定の保存が完了しました");

            // 通知音の設定を再初期化
            Logger.Log("通知音を再初期化します...");
            _mainForm.ReinitializeSound();
            Logger.Log("通知音の再初期化が完了しました");

            // 設定が正常に保存されたことを確認
            _configManager.LoadConfig();
            if (_configManager.CurrentConfig.CustomSoundFilePath != newSoundPath)
            {
                throw new InvalidOperationException("設定の保存に失敗しました。");
            }

            MessageBox.Show(
                "設定を保存しました。",
                "保存完了",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Log($"設定の保存中にエラーが発生しました: {ex.Message}");
            if (ex.InnerException != null)
            {
                Logger.Log($"内部エラー: {ex.InnerException.Message}");
            }
            MessageBox.Show(
                $"設定の保存に失敗しました。\n{ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            this.DialogResult = DialogResult.None;
        }
    }

    /// <summary>
    /// 通知設定ダイアログを表示します
    /// </summary>
    /// <returns>通知を有効にする場合はtrue、無効にする場合はfalse</returns>
    private bool ShowNotificationSettingDialog()
    {
        var result = MessageBox.Show(
            "OCR結果をポップアップ通知で表示しますか？\n\n" +
            "「はい」：OCR結果をポップアップ通知で表示します\n" +
            "「いいえ」：OCR結果をクリップボードにコピーするだけで通知は表示しません",
            "通知設定",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
            
        return result == DialogResult.Yes;
    }
    
    /// <summary>
    /// 通知音設定ダイアログを表示します
    /// </summary>
    /// <returns>通知音を有効にする場合はtrue、無効にする場合はfalse</returns>
    private bool ShowSoundSettingDialog()
    {
        var result = MessageBox.Show(
            "OCR成功時に通知音を鳴らしますか？\n\n" +
            "「はい」：OCR成功時に通知音を鳴らします\n" +
            "「いいえ」：通知音を鳴らしません",
            "通知音設定",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
            
        return result == DialogResult.Yes;
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