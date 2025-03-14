using GeminiOcrCapture.Core;
using System.Drawing;
using System.Diagnostics;
using System.Media;
using System.IO;
using System;

namespace GeminiOcrCapture;

public partial class MainForm : Form
{
    private readonly ConfigManager _configManager = null!;
    private GeminiService? _geminiService;
    private readonly ScreenCapture _screenCapture = null!;
    private readonly ErrorHandler _errorHandler = null!;
    private SoundPlayer? _soundPlayer;
    private bool _customSoundAvailable = false;
    private NotifyIcon _notifyIcon = null!;
    private ContextMenuStrip _contextMenu = null!;

    public MainForm()
    {
        Logger.Log("MainForm: アプリケーションを初期化します...");
        
        InitializeComponent();
        InitializeNotifyIcon();

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
    public void InitializeSound()
    {
        Logger.Log("InitializeSound: 通知音の初期化を開始します...");
        try
        {
            // 既存のSoundPlayerを破棄
            if (_soundPlayer != null)
            {
                Logger.Log("InitializeSound: 既存のSoundPlayerを破棄します");
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
            
            // 設定ファイルからカスタム通知音ファイルのパスを取得
            string? customSoundFilePath = _configManager.CurrentConfig.CustomSoundFilePath;
            Logger.Log($"InitializeSound: 設定された通知音ファイルパス: {customSoundFilePath ?? "未設定"}");
            
            // カスタム通知音ファイルのパスが設定されていて、ファイルが存在する場合
            if (!string.IsNullOrEmpty(customSoundFilePath) && File.Exists(customSoundFilePath))
            {
                Logger.Log($"InitializeSound: 通知音ファイルが存在することを確認: {customSoundFilePath}");
                _soundPlayer = new SoundPlayer(customSoundFilePath);
                try
                {
                    Logger.Log("InitializeSound: 通知音ファイルの読み込みを開始...");
                    // 同期的に読み込み
                    _soundPlayer.Load();
                    _customSoundAvailable = true;
                    Logger.Log("InitializeSound: 通知音ファイルの読み込みが完了しました");
                }
                catch (Exception loadEx)
                {
                    Logger.Log($"InitializeSound: 通知音の読み込みに失敗: {loadEx.Message}");
                    _soundPlayer.Dispose();
                    _soundPlayer = null;
                    _customSoundAvailable = false;
                }
            }
            else
            {
                Logger.Log("InitializeSound: 通知音ファイルが設定されていないか、見つかりません");
                _customSoundAvailable = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"InitializeSound: 通知音の初期化中にエラー発生: {ex.Message}");
            _customSoundAvailable = false;
            if (_soundPlayer != null)
            {
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
        }
        Logger.Log($"InitializeSound: 初期化完了。カスタム通知音の利用可能状態: {_customSoundAvailable}");
    }
    
    /// <summary>
    /// 通知音を再初期化します
    /// </summary>
    public void ReinitializeSound()
    {
        Logger.Log("ReinitializeSound: 通知音の再初期化を開始します...");
        try
        {
            // 既存のSoundPlayerを破棄
            if (_soundPlayer != null)
            {
                Logger.Log("ReinitializeSound: 既存のSoundPlayerを破棄します");
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
            
            // 設定を最新の状態に更新
            _configManager.LoadConfig();
            Logger.Log("ReinitializeSound: 設定を再読み込みしました");
            
            // カスタム通知音ファイルのパスを取得
            string? customSoundFilePath = _configManager.CurrentConfig.CustomSoundFilePath;
            Logger.Log($"ReinitializeSound: 設定された通知音ファイルパス: {customSoundFilePath ?? "未設定"}");
            
            // カスタム通知音ファイルのパスが設定されていて、ファイルが存在する場合
            if (!string.IsNullOrEmpty(customSoundFilePath) && File.Exists(customSoundFilePath))
            {
                Logger.Log($"ReinitializeSound: 通知音ファイルが存在することを確認: {customSoundFilePath}");
                _soundPlayer = new SoundPlayer(customSoundFilePath);
                try
                {
                    Logger.Log("ReinitializeSound: 通知音ファイルの読み込みを開始...");
                    // 同期的に読み込み
                    _soundPlayer.Load();
                    _customSoundAvailable = true;
                    Logger.Log("ReinitializeSound: 通知音ファイルの読み込みが完了しました");
                }
                catch (Exception loadEx)
                {
                    Logger.Log($"ReinitializeSound: 通知音の読み込みに失敗: {loadEx.Message}");
                    _soundPlayer.Dispose();
                    _soundPlayer = null;
                    _customSoundAvailable = false;
                }
            }
            else
            {
                Logger.Log("ReinitializeSound: 通知音ファイルが設定されていないか、見つかりません");
                _customSoundAvailable = false;
            }
            
            Logger.Log($"ReinitializeSound: 再初期化完了。カスタム通知音の利用可能状態: {_customSoundAvailable}");
        }
        catch (Exception ex)
        {
            Logger.Log($"ReinitializeSound: 通知音の再初期化中にエラー発生: {ex.Message}");
            _customSoundAvailable = false;
            if (_soundPlayer != null)
            {
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
        }
    }
    
    /// <summary>
    /// 通知音を再生します
    /// </summary>
    private void PlayNotificationSound()
    {
        Logger.Log("PlayNotificationSound: 通知音の再生を開始します...");
        
        // 設定を最新の状態に更新
        _configManager.LoadConfig();
        
        // 通知音ファイルの再読み込み
        string? customSoundFilePath = _configManager.CurrentConfig.CustomSoundFilePath;
        Logger.Log($"PlayNotificationSound: 設定された通知音ファイルパス: {customSoundFilePath ?? "未設定"}");
        
        // 通知音ファイルが変更されている場合、または_customSoundAvailableがfalseの場合は再読み込み
        if (!string.IsNullOrEmpty(customSoundFilePath) && File.Exists(customSoundFilePath) && 
            (!_customSoundAvailable || _soundPlayer == null))
        {
            Logger.Log($"PlayNotificationSound: 通知音ファイルを再読み込みします: {customSoundFilePath}");
            
            // 既存のSoundPlayerを破棄
            if (_soundPlayer != null)
            {
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
            
            try
            {
                _soundPlayer = new SoundPlayer(customSoundFilePath);
                _soundPlayer.Load();
                _customSoundAvailable = true;
                Logger.Log("PlayNotificationSound: 通知音ファイルの読み込みが完了しました");
            }
            catch (Exception ex)
            {
                Logger.Log($"PlayNotificationSound: 通知音ファイルの読み込みに失敗: {ex.Message}");
                _customSoundAvailable = false;
                if (_soundPlayer != null)
                {
                    _soundPlayer.Dispose();
                    _soundPlayer = null;
                }
            }
        }
        
        Logger.Log($"PlayNotificationSound: 現在の設定 - PlaySoundOnOcrSuccess: {_configManager.CurrentConfig.PlaySoundOnOcrSuccess}, CustomSoundAvailable: {_customSoundAvailable}");
        
        if (_configManager.CurrentConfig.PlaySoundOnOcrSuccess)
        {
            try
            {
                if (_customSoundAvailable && _soundPlayer != null)
                {
                    try
                    {
                        Logger.Log("PlayNotificationSound: カスタム通知音を再生します...");
                        // カスタム通知音を再生（同期的に）
                        _soundPlayer.PlaySync();
                        Logger.Log("PlayNotificationSound: カスタム通知音の再生が完了しました");
                    }
                    catch (Exception playEx)
                    {
                        Logger.Log($"PlayNotificationSound: カスタム通知音の再生に失敗: {playEx.Message}");
                        SystemSounds.Beep.Play();
                    }
                }
                else
                {
                    Logger.Log("PlayNotificationSound: 標準の通知音を再生します");
                    SystemSounds.Beep.Play();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"PlayNotificationSound: 通知音の再生中にエラー発生: {ex.Message}");
                SystemSounds.Beep.Play();
            }
        }
        else
        {
            Logger.Log("PlayNotificationSound: 通知音は無効化されています");
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
                if (_geminiService == null)
                {
                    throw new InvalidOperationException("Gemini APIサービスが初期化されていません。");
                }
                
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
                    Console.WriteLine($"GeminiServiceの破棄中にエラーが発生しました: {disposeEx.Message}");
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
            Console.WriteLine("OCR処理を再試行します。");
            
            // _geminiServiceがnullでないことを確認
            if (_geminiService == null)
            {
                throw new InvalidOperationException("Gemini APIサービスが初期化されていません。");
            }
            
            var text = await _geminiService.AnalyzeImageAsync(capturedImage);
            Console.WriteLine("OCR処理が成功しました。");
            
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
            Console.WriteLine($"再試行中にエラーが発生しました: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部例外: {ex.InnerException.Message}");
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

    /// <summary>
    /// 通知アイコンとコンテキストメニューを初期化します
    /// </summary>
    private void InitializeNotifyIcon()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("設定", null, (s, e) => ShowSettingsForm());
        _contextMenu.Items.Add("-");
        _contextMenu.Items.Add("終了", null, (s, e) => Application.Exit());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Gemini OCR Capture",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => ShowSettingsForm();
    }

    /// <summary>
    /// 設定画面を表示します
    /// </summary>
    private void ShowSettingsForm()
    {
        Logger.Log("設定画面を表示します...");
        using var form = new SettingsForm(_configManager, this);
        if (form.ShowDialog() == DialogResult.OK)
        {
            // 設定が保存された場合、通知音を再初期化
            Logger.Log("設定が保存されました。通知音を再初期化します...");
            ReinitializeSound();
            Logger.Log("通知音の再初期化が完了しました");
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // フォームを非表示にする
        this.Visible = false;
        this.ShowInTaskbar = false;
    }
}