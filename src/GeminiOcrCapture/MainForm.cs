using GeminiOcrCapture.Core;
using System.Drawing;

namespace GeminiOcrCapture;

public partial class MainForm : Form
{
    private readonly ConfigManager _configManager = null!;
    private GeminiService _geminiService = null!;
    private readonly ScreenCapture _screenCapture = null!;
    private readonly ErrorHandler _errorHandler = null!;

    public MainForm()
    {
        InitializeComponent();

        try
        {
            // 設定マネージャーとエラーハンドラーを初期化
            _configManager = new ConfigManager();
            _errorHandler = new ErrorHandler();
            _screenCapture = new ScreenCapture();

            // APIキーの確認と設定
            if (!CheckAndSetupApiKey())
            {
                Application.Exit();
                return;
            }

            // 設定ファイルを再読み込み（APIキーが確実に読み込まれるようにするため）
            _configManager.LoadConfig();

            // APIキーが設定されたら、GeminiServiceを初期化
            InitializeGeminiService();
            
            // イベントの初期化
            InitializeEvents();
            
            // APIキーの検証が完了するまで少し待機
            System.Threading.Thread.Sleep(500);
            
            // キャプチャを開始
            _screenCapture.StartCapture();
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

    /// <summary>
    /// GeminiServiceを初期化します
    /// </summary>
    private void InitializeGeminiService()
    {
        try
        {
            // APIキーが設定されているか再確認
            if (string.IsNullOrEmpty(_configManager.CurrentConfig.ApiKey))
            {
                throw new InvalidOperationException("APIキーが設定されていません。設定画面から設定してください。");
            }
            
            _geminiService = new GeminiService(_configManager);
            
            // APIキーの検証を同期的に行う
            Task.Run(async () => {
                try {
                    // 簡単なテスト呼び出しを行い、APIキーが有効か確認
                    using var image = new Bitmap(1, 1);
                    await _geminiService.AnalyzeImageAsync(image);
                    return true;
                }
                catch {
                    return false;
                }
            }).Wait(1000); // 最大1秒待機
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Gemini APIサービスの初期化に失敗しました。" + ex.Message, ex);
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

    /// <summary>
    /// APIキーが設定されているかチェックし、設定されていない場合は設定ダイアログを表示します
    /// </summary>
    /// <returns>APIキーが正常に設定された場合はtrue、キャンセルされた場合はfalse</returns>
    private bool CheckAndSetupApiKey()
    {
        if (string.IsNullOrEmpty(_configManager.CurrentConfig.ApiKey))
        {
            // 初回起動時の説明メッセージを表示
            MessageBox.Show(
                "Gemini OCR Captureへようこそ！\n\n" +
                "このアプリケーションを使用するには、Google AI StudioからGemini API Keyを取得して設定する必要があります。\n\n" +
                "次のダイアログでAPIキーを入力してください。\n" +
                "キャンセルした場合、アプリケーションは終了します。",
                "初回設定",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // APIキー設定ダイアログを表示
            using var form = new ApiKeyForm(_configManager);
            if (form.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(
                    "APIキーが設定されていないため、アプリケーションを終了します。\n" +
                    "次回起動時に再度設定することができます。",
                    "終了",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            // APIキーが正常に設定された場合の確認メッセージ
            MessageBox.Show(
                "APIキーが正常に設定されました。\n" +
                "スクリーンキャプチャを開始します。",
                "設定完了",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        return true;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // フォームを非表示にする
        this.Visible = false;
        this.ShowInTaskbar = false;
    }
}