# Gemini OCR Capture

Gemini OCR Captureは、Google Gemini 2.0 Flash APIを使用して、画面キャプチャからテキストを抽出するシンプルで使いやすいOCRツールです。

## 機能

- **画面キャプチャ**: 選択範囲のキャプチャが可能
- **OCR処理**: Google Gemini 2.0 Flash APIを使用した高精度なテキスト抽出
- **カスタム通知音**: OCR成功時に任意の通知音を再生可能
- **セキュリティ機能**:
  - **APIキーの保護**: Windows Data Protection API (DPAPI)を使用してAPIキーを暗号化
  - **ユーザー固有の暗号化**: 各ユーザーのWindowsアカウントに紐づけられた暗号化を使用
  - **安全な保存**: 暗号化されたAPIキーはローカルの設定ファイルに保存

## 必要条件

- Windows 10/11
- .NET 9.0以上
- Google Cloud Platform アカウント
- Gemini APIキー

## インストール方法

1. リリースページから最新のバージョンをダウンロード
2. ダウンロードしたZIPファイルを解凍
3. 解凍したフォルダ内の`GeminiOcrCapture.exe`を実行

## 初期設定

### Google Cloud PlatformでのAPIキー取得

1. [Google Cloud Console](https://console.cloud.google.com/)にアクセス
2. プロジェクトを作成または選択
3. 「APIとサービス」→「ライブラリ」を選択
4. 「Gemini API」を検索して有効化
5. 「APIとサービス」→「認証情報」を選択
6. 「認証情報を作成」→「APIキー」を選択
7. 作成されたAPIキーをコピー

### アプリケーションの設定(初回起動時)

1. アプリケーションを起動
2. 設定画面が開く
3. コピーしたAPIキーを入力
4. 必要に応じて通知設定などを実施

### アプリケーションの設定(初回起動時以外)

1. config.jsonファイルを直接編集する
ex. config.jsonを削除してアプリケーションを起動すると、初回起動時と同様のダイアログが表示されます

## 使い方

### 範囲選択キャプチャ

1. アプリケーションを起動
2. マウスでキャプチャしたい範囲をドラッグして選択
3. 選択範囲がキャプチャされ、OCR処理が自動的に開始
4. 抽出されたテキストがクリップボードに登録

### カスタム通知音の設定

1. 初回設定画面から「OCR成功時に通知音を鳴らす」を選択
2. 「通知音ファイル」欄に使用したい.wavファイルのパスを入力するか、「参照...」ボタンでファイルを選択する
3. 「保存」ボタンをクリックして設定を保存する

※空白の場合は標準のビープ音が使用されます。

## おすすめ

### ショートカットからの起動

1. PowerToysのキーボードショートカットなど、任意のツールを用いて起動できる様にしておくと便利です

## トラブルシューティング

### APIキーエラー

- APIキーが正しく設定されているか確認
- Google Cloud ConsoleでGemini APIが有効化されているか確認
- APIキーに適切な権限が設定されているか確認

### OCR処理エラー

- インターネット接続を確認
- 画像サイズが大きすぎる場合は、より小さな範囲を選択
- APIクォータを超過している場合は、Google Cloud Consoleで課金設定を確認

### その他のエラー

- アプリケーションを再起動
- 最新バージョンにアップデート
- エラーログを確認（アプリケーションフォルダ内の`error.log`）

## 開発者向け情報

### プロジェクト構成

- **GeminiOcrCapture**: メインアプリケーション（UI）
- **GeminiOcrCapture.Core**: コアライブラリ（OCR処理、設定管理など）
- **GeminiOcrCapture.Tests**: テストプロジェクト

### ビルド方法

```powershell
# リポジトリのクローン
git clone https://github.com/thakyuu/GeminiOcrCapture.git
cd GeminiOcrCapture

# ビルド
dotnet build --configuration Release

# 実行
dotnet run --project src\GeminiOcrCapture
```

### テスト実行

```powershell
dotnet test
```

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。詳細は[LICENSE](LICENSE)ファイルを参照してください。

## 謝辞

- [Google Gemini API](https://ai.google.dev/gemini-api)
- [.NET](https://dotnet.microsoft.com/)

## 連絡先

バグ報告や機能リクエストは、GitHubのIssueページからお願いします。 
