using GeminiOcrCapture.Core;
using System.Drawing;

namespace GeminiOcrCapture;

/// <summary>
/// APIエラーが発生した際に表示するダイアログ
/// </summary>
public class ApiErrorDialog : Form
{
    private readonly ConfigManager _configManager;
    private readonly string _errorMessage;
    private Button _retryButton = new Button();
    private Button _settingsButton = new Button();
    private Button _exitButton = new Button();

    /// <summary>
    /// APIエラーダイアログのコンストラクタ
    /// </summary>
    /// <param name="configManager">設定マネージャーのインスタンス</param>
    /// <param name="errorMessage">エラーメッセージ</param>
    public ApiErrorDialog(ConfigManager configManager, string errorMessage)
    {
        _configManager = configManager;
        _errorMessage = FormatErrorMessage(errorMessage);
        InitializeComponents();
    }

    /// <summary>
    /// エラーメッセージを整形します
    /// </summary>
    private string FormatErrorMessage(string message)
    {
        // エラーメッセージが長すぎる場合は省略
        if (message.Length > 200)
        {
            message = message.Substring(0, 197) + "...";
        }
        
        // 一般的なエラーメッセージをユーザーフレンドリーなメッセージに変換
        if (message.Contains("Unauthorized") || message.Contains("無効"))
        {
            return "APIキーが無効です。正しいAPIキーを設定してください。";
        }
        else if (message.Contains("quota") || message.Contains("クォータ"))
        {
            return "APIクォータを超過しました。Google Cloud Consoleで課金設定を確認してください。";
        }
        else if (message.Contains("model not found") || message.Contains("モデルが見つかりません"))
        {
            return "Gemini 2.0 Flashモデルが利用できません。Google Cloud ConsoleでGemini APIが有効化されているか確認してください。";
        }
        else if (message.Contains("network") || message.Contains("ネットワーク"))
        {
            return "ネットワークエラーが発生しました。インターネット接続を確認してください。";
        }
        
        return message;
    }

    /// <summary>
    /// フォームのコンポーネントを初期化します
    /// </summary>
    private void InitializeComponents()
    {
        this.Text = "APIエラー";
        this.Width = 450;
        this.Height = 250;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        var titleLabel = new Label
        {
            Text = "Gemini APIエラー",
            Location = new Point(10, 10),
            Width = 430,
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold)
        };

        var errorLabel = new Label
        {
            Text = _errorMessage,
            Location = new Point(10, 40),
            Width = 430,
            Height = 80,
            AutoSize = false
        };

        var questionLabel = new Label
        {
            Text = "どうしますか？",
            Location = new Point(10, 130),
            Width = 430
        };

        _retryButton = new Button
        {
            Text = "再試行",
            Location = new Point(90, 170),
            Width = 100,
            DialogResult = DialogResult.Retry
        };

        _settingsButton = new Button
        {
            Text = "APIキー設定",
            Location = new Point(200, 170),
            Width = 100,
            DialogResult = DialogResult.Yes
        };
        _settingsButton.Click += SettingsButton_Click;

        _exitButton = new Button
        {
            Text = "終了",
            Location = new Point(310, 170),
            Width = 100,
            DialogResult = DialogResult.Cancel
        };

        this.Controls.AddRange(new Control[] {
            titleLabel,
            errorLabel,
            questionLabel,
            _retryButton,
            _settingsButton,
            _exitButton
        });

        this.AcceptButton = _retryButton;
        this.CancelButton = _exitButton;
    }

    /// <summary>
    /// APIキー設定ボタンがクリックされたときの処理
    /// </summary>
    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        try
        {
            // APIキー設定ダイアログを表示
            using var form = new ApiKeyForm(_configManager);
            if (form.ShowDialog() == DialogResult.OK)
            {
                // 設定が更新された場合は再読み込み
                _configManager.LoadConfig();
                
                // 確認メッセージを表示
                MessageBox.Show(
                    "APIキーが更新されました。再試行します。",
                    "設定完了",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                
                // ダイアログの結果をYesに設定（再試行を行うため）
                this.DialogResult = DialogResult.Yes;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"APIキーの設定中にエラーが発生しました。\n{ex.Message}",
                "エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
} 