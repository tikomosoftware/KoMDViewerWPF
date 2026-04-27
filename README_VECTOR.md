# KoMDViewer v1.0

📝 軽量で美しいMarkdownビューアー＆エディター

## 🎯 特徴

- **モダンUI**: WinUI 3 + Mica背景による洗練されたデザイン
- **高品質レンダリング**: Markdig + highlight.js によるシンタックスハイライト対応
- **編集モード**: CodeMirror 6 ベースのMarkdownエディター内蔵
- **PDF出力**: 表示中のMarkdownをPDFにエクスポート
- **テーマ切り替え**: ライト・ダークテーマ対応
- **ドラッグ&ドロップ**: ファイルをウィンドウにドロップして開ける
- **最近のファイル**: 最近開いたファイルの履歴管理
- **コマンドライン対応**: ファイルパスを引数に渡して直接開ける

## 📥 インストール・使用方法

### 必要なランタイム

KoMDViewerの実行には、以下の2つのランタイムが必要です。初回のみインストールしてください。

#### 1. .NET 9.0 Desktop Runtime
1. [.NET 9.0 ダウンロードページ](https://dotnet.microsoft.com/download/dotnet/9.0) を開く
2. 「.NET Desktop Runtime 9.0.x」の **Windows x64** インストーラーをダウンロード
3. ダウンロードしたインストーラーを実行

#### 2. Windows App SDK Runtime
1. [Windows App SDK ダウンロードページ](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) を開く
2. 最新の安定版リリースから **Runtime** のダウンロードリンクをクリック
3. ダウンロードしたインストーラーを実行

> 💡 どちらも一度インストールすれば、以降のアップデートでは再インストール不要です。

### インストール手順
1. ダウンロードしたZIPファイルを任意のフォルダに展開
2. `KoMDViewer.exe` を実行

## 🚀 使い方

### ファイルを開く
- **ドラッグ&ドロップ**: Markdownファイルをウィンドウにドロップ
- **メニューから**: ファイル → 開く
- **ショートカット**: `Ctrl + O`
- **コマンドライン**: `KoMDViewer.exe "path/to/file.md"`

### キーボードショートカット

| キー | 機能 |
|------|------|
| `Ctrl + O` | ファイルを開く |
| `Ctrl + S` | 保存（編集モード時） |
| `Ctrl + Shift + E` | PDFにエクスポート |
| `F2` | 編集モード切り替え |
| `Esc` | 編集モード終了 |

### 編集モード
1. Markdownファイルを開いた状態で `F2` キーまたは 表示 → 編集モード
2. CodeMirror 6 ベースのエディターでMarkdownを編集
3. `Ctrl + S` で保存
4. `Esc` で表示モードに戻る

### PDF出力
1. Markdownファイルを開いた状態で `Ctrl + Shift + E` またはファイル → PDFに出力
2. 保存先を選択
3. ライトテーマでPDFが生成される

## 📁 対応ファイル形式

- Markdown (`.md`, `.markdown`)
- テキスト (`.txt`)
- その他 (`.mdown`, `.mkd`, `.mkdn`)

## ⚙️ 機能詳細

### テーマ
- **ライトテーマ**: GitHub風の明るいデザイン
- **ダークテーマ**: 目に優しいダークデザイン
- Mica背景による半透明効果

### 最近のファイル
- 最大10件の履歴を自動保存
- 存在しないファイルは開く際にメッセージ表示＆履歴から自動削除
- 履歴のクリア機能

## 🔧 トラブルシューティング

### アプリが起動しない
- **ランタイムが未インストール**: .NET 9.0 Desktop Runtime と Windows App SDK Runtime の両方がインストールされているか確認してください。どちらかが欠けていると、exeをダブルクリックしても何も起こらずサイレントにクラッシュします
  - [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
  - [Windows App SDK Runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)
- **WebView2 Runtime**: Windows 10の場合、Microsoft Edge WebView2 Runtimeが必要です
- Windows 10 version 1809 以降が必要です

### Markdownが表示されない
- 対応ファイル形式であることを確認してください
- ファイルのエンコーディングがUTF-8であることを確認してください

### PDFが出力できない
- 出力先のフォルダに書き込み権限があるか確認してください

## 📞 サポート

- **E-Mail**: tikomo@gmail.com
- **HP**: https://tikomosoftware.github.io

## 📋 更新履歴

### v1.0 (2026-04-18)
- 初回リリース
- Markdownファイルの表示・編集
- PDF出力機能
- ライト・ダークテーマ対応
- ドラッグ&ドロップ対応
- 最近のファイル履歴
- コマンドライン引数対応

---

**KoMDViewer** - シンプルで美しいMarkdown体験を提供します 📝✨

© 2026 tikomo software
