using GeminiOcrCapture.Core;
using System.Drawing;
using System.Diagnostics;
using System.Media;
using System.IO;

namespace GeminiOcrCapture;

public partial class MainForm : Form
{
    private readonly ConfigManager _configManager = null!;
    private GeminiService _geminiService = null!;
    private readonly ScreenCapture _screenCapture = null!;
    private readonly ErrorHandler _errorHandler = null!;
    private SoundPlayer _soundPlayer = null!;
    private bool _customSoundAvailable = false;

    public MainForm()
    {
        InitializeComponent();

        try
        {
            // 設定マネージャーとエラーハンドラーを初期化
            _configManager = new ConfigManager();
            _errorHandler = new ErrorHandler();
            _screenCapture = new ScreenCapture();
            
            // 通知音の初期化
            InitializeSound();

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
    /// 通知音を初期化します
    /// </summary>
    private void InitializeSound()
    {
        try
        {
            // 設定ファイルからカスタム通知音ファイルのパスを取得
            string? customSoundFilePath = _configManager.CurrentConfig.CustomSoundFilePath;
            
            // カスタム通知音ファイルのパスが設定されていて、ファイルが存在する場合
            if (!string.IsNullOrEmpty(customSoundFilePath) && File.Exists(customSoundFilePath))
            {
                _soundPlayer = new SoundPlayer(customSoundFilePath);
                _soundPlayer.LoadAsync(); // 非同期で読み込み
                _customSoundAvailable = true;
                Debug.WriteLine($"カスタム通知音を読み込みました: {customSoundFilePath}");
            }
            else
            {
                Debug.WriteLine("カスタム通知音ファイルが設定されていないか、ファイルが見つかりません。標準の通知音を使用します。");
                _customSoundAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"通知音の初期化中にエラーが発生しました: {ex.Message}");
            _customSoundAvailable = false;
        }
    }
    
    /// <summary>
    /// 通知音を再生します
    /// </summary>
    private void PlayNotificationSound()
    {
        if (_configManager.CurrentConfig.PlaySoundOnOcrSuccess)
        {
            try
            {
                if (_customSoundAvailable && _soundPlayer != null)
                {
                    // カスタム通知音を再生
                    _soundPlayer.Play();
                    Debug.WriteLine("カスタム通知音を再生しました");
                }
                else
                {
                    // 標準の通知音を再生
                    SystemSounds.Beep.Play();
                    Debug.WriteLine("標準の通知音を再生しました");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"通知音の再生中にエラーが発生しました: {ex.Message}");
                // エラーが発生した場合は標準の通知音を再生
                SystemSounds.Beep.Play();
            }
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
                
                // OCR成功時に通知音を鳴らす
                PlayNotificationSound();
                
                if (_configManager.CurrentConfig.DisplayOcrResult)
                {
                    MessageBox.Show(text, "OCR結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                Application.Exit();
            }
            catch (Exception ex)
            {
                // APIエラーの場合は専用ダイアログを表示
                if (IsApiError(ex))
                {
                    HandleApiError(ex, image);
                }
                else
                {
                    _errorHandler.HandleError(ex, "OCR処理に失敗しました。");
                    Application.Exit();
                }
            }
        };

        _screenCapture.CaptureCancelled += (sender, e) =>
        {
            Application.Exit();
        };
    }

    /// <summary>
    /// 例外がAPIエラーかどうかを判定します
    /// </summary>
    private bool IsApiError(Exception ex)
    {
        // APIエラーの判定条件
        return ex is InvalidOperationException && 
               (ex.Message.Contains("API") || 
                ex.Message.Contains("キー") || 
                ex.Message.Contains("認証"));
    }

    /// <summary>
    /// APIエラーを処理します
    /// </summary>
    private void HandleApiError(Exception ex, Image capturedImage)
    {
        // エラーをログに記録
        _errorHandler.LogError(ex, "Gemini APIエラーが発生しました。");

        // APIエラーダイアログを表示
        using var dialog = new ApiErrorDialog(_configManager, ex.Message);
        var result = dialog.ShowDialog();

        switch (result)
        {
            case DialogResult.Retry:
                // 再試行
                RetryOcrProcess(capturedImage);
                break;
            
            case DialogResult.Yes:
                // APIキーが更新されたので再試行
                RetryOcrProcess(capturedImage);
                break;
            
            default:
                // 終了
                Application.Exit();
                break;
        }
    }

    /// <summary>
    /// OCR処理を再試行します
    /// </summary>
    private async void RetryOcrProcess(Image capturedImage)
    {
        try
        {
            // 設定ファイルを再読み込み（APIキーが確実に読み込まれるようにするため）
            _configManager.LoadConfig();
            
            // GeminiServiceを再初期化する前に、既存のインスタンスを破棄
            if (_geminiService != null)
            {
                try
                {
                    _geminiService.Dispose();
                }
                catch (Exception disposeEx)
                {
                    Debug.WriteLine($"GeminiServiceの破棄中にエラーが発生しました: {disposeEx.Message}");
                }
                // nullに設定する前に型を明示的に指定
                _geminiService = null!;
            }
            
            try
            {
                // GeminiServiceを再初期化
                InitializeGeminiService();
            }
            catch (Exception initEx)
            {
                throw new InvalidOperationException($"Gemini APIサービスの再初期化に失敗しました: {initEx.Message}", initEx);
            }
            
            // APIキーの検証が完了するまで少し待機
            System.Threading.Thread.Sleep(500);
            
            // OCR処理を再実行
            Debug.WriteLine("OCR処理を再試行します。");
            
            // _geminiServiceがnullでないことを確認
            if (_geminiService == null)
            {
                throw new InvalidOperationException("Gemini APIサービスが初期化されていません。");
            }
            
            var text = await _geminiService.AnalyzeImageAsync(capturedImage);
            Debug.WriteLine("OCR処理が成功しました。");
            
            Clipboard.SetText(text);
            
            // OCR成功時に通知音を鳴らす
            PlayNotificationSound();
            
            if (_configManager.CurrentConfig.DisplayOcrResult)
            {
                MessageBox.Show(text, "OCR結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Application.Exit();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"再試行中にエラーが発生しました: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"内部例外: {ex.InnerException.Message}");
            }
            
            // APIエラーの場合は専用ダイアログを表示
            if (IsApiError(ex))
            {
                HandleApiError(ex, capturedImage);
            }
            else
            {
                // より詳細なエラーメッセージを表示
                string errorMessage = $"OCR処理の再試行に失敗しました。\n\nエラー詳細: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\n内部エラー: {ex.InnerException.Message}";
                }
                
                MessageBox.Show(
                    errorMessage,
                    "エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                _errorHandler.LogError(ex, "OCR処理の再試行に失敗しました。");
                Application.Exit();
            }
        }
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