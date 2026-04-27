# KoMDViewer v1.0

📝 軽量で美しいMarkdownビューアー＆エディター

![KoMDViewer](https://img.shields.io/badge/Platform-Windows-blue) ![.NET](https://img.shields.io/badge/.NET-9.0-purple) ![License](https://img.shields.io/badge/License-MIT-green) ![Version](https://img.shields.io/badge/Version-1.0-orange)

## 🎯 特徴

- **モダンUI**: WinUI 3 + Mica背景による洗練されたデザイン
- **高品質レンダリング**: Markdig + highlight.js によるシンタックスハイライト対応
- **編集モード**: CodeMirror 6 ベースのMarkdownエディター内蔵
- **PDF出力**: 表示中のMarkdownをPDFにエクスポート
- **テーマ切り替え**: ライト・ダークテーマ対応
- **ドラッグ&ドロップ**: ファイルをウィンドウにドロップして開ける
- **最近のファイル**: 最近開いたファイルの履歴管理
- **コマンドライン対応**: ファイルパスを引数に渡して直接開ける

## 📥 ダウンロード・インストール

### システム要件
- **OS**: Windows 10 version 1809 以降 / Windows 11
- **[.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)**: 必須
- **[Windows App SDK Runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)**: 必須（未インストールの場合、exeが無反応でクラッシュします）
- **WebView2 Runtime**: Microsoft Edge WebView2（Windows 11は標準搭載、Windows 10は自動インストール）

### ランタイムのインストール手順

KoMDViewerを初めて使う場合、以下の2つのランタイムを事前にインストールしてください。

#### 1. .NET 9.0 Desktop Runtime
1. [.NET 9.0 ダウンロードページ](https://dotnet.microsoft.com/download/dotnet/9.0) を開く
2. 「.NET Desktop Runtime 9.0.x」の **Windows x64** インストーラーをダウンロード
3. ダウンロードしたインストーラーを実行

#### 2. Windows App SDK Runtime
1. [Windows App SDK ダウンロードページ](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) を開く
2. 最新の安定版リリースから **Runtime** のダウンロードリンクをクリック（「Downloads for the Windows App SDK」セクション内）
3. ダウンロードしたインストーラーを実行

> 💡 どちらも一度インストールすれば、以降のアップデートでは再インストール不要です。

### インストール手順
1. [Releases](../../releases)から `KoMDViewer-v*-release.zip` をダウンロード
2. ZIPファイルを任意のフォルダに展開
3. `KoMDViewer.exe` を実行

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

### Markdownレンダリング
- CommonMark準拠 + 拡張構文（テーブル、タスクリスト、脚注など）
- コードブロックのシンタックスハイライト（highlight.js）
- 数式、絵文字などの拡張機能

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

### Markdownが表示されない
- 対応ファイル形式であることを確認してください
- ファイルのエンコーディングがUTF-8であることを確認してください

### PDFが出力できない
- WebView2が正常に動作しているか確認してください
- 出力先のフォルダに書き込み権限があるか確認してください

## 🛠️ 開発者向け情報

### ビルド方法

```bash
# 依存関係の復元
dotnet restore

# デバッグビルド
dotnet build

# 実行
dotnet run
```

### リリースビルド

```bash
# ビルドスクリプトを使用
.\build-and-package.ps1 -Version "1.0"
```

### ビルド・配布に関する技術的な注意事項

本プロジェクトでは、ランタイム同梱の自己完結型（self-contained）配布を検討しましたが、`dotnet` CLI によるビルドでは以下の技術的制約があり、現時点ではフレームワーク依存版（framework-dependent）のみの配布としています。

> **補足**: Windows App SDK 1.1 以降、`WindowsAppSDKSelfContained=true` による self-contained 配布は[公式にサポートされている機能](https://docs.microsoft.com/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps)です。ただし公式ドキュメントに「`dotnet publish` is not yet supported」と明記されており、**Visual Studio のビルドパイプラインが前提**となっています。VS 経由であれば self-contained 版を作成できる可能性があります。

#### 1. dotnet publish / dotnet build --self-contained で .xbf ファイルが出力されない

WinUI 3 の非パッケージアプリ（`WindowsPackageType=None`）では、`dotnet publish` および `dotnet build --self-contained` 実行時に XAMLコンパイル済みリソース（`.xbf` ファイル）が出力に含まれません。`.xbf` が欠落すると、exe は `0xC0000142`（DLL Initialization Failed）でサイレントにクラッシュします。

これは `EnableCoreMrtTooling` が Visual Studio の MSBuild タスク（`Microsoft.Build.Packaging.Pri.Tasks.dll`）に依存しており、`dotnet` CLI 単体では動作しないことが原因です。

このため、ビルドスクリプトでは `dotnet publish` ではなく通常の `dotnet build` の出力をパッケージングし、`.xbf` ファイルが含まれるようにしています。

#### 2. WindowsAppSDKSelfContained=true で CoreMessagingXP.dll が競合する

`WindowsAppSDKSelfContained=true` を指定すると、Windows App SDK のネイティブ DLL（`CoreMessagingXP.dll` 等）がアプリフォルダに同梱されます。しかし、システムに Windows App SDK Runtime がインストール済みの環境では、同梱された DLL とシステムの DLL がバージョン競合を起こし、`0xc0000602`（STATUS_FAIL_FAST_EXCEPTION）でクラッシュします。

この問題はランタイム未インストールのクリーンな環境では発生しない可能性がありますが、開発マシンでの動作確認ができないため、現時点では Windows App SDK の同梱を見送っています。

### 使用ライブラリ
- **Markdig** v0.40.0 — Markdownパーサー（BSD-2-Clause）
- **CodeMirror 6** — テキストエディター（MIT）
- **highlight.js** v11.11.1 — シンタックスハイライト（BSD-3-Clause）
- **Microsoft.Web.WebView2** — Webコンテンツ表示
- **Microsoft.WindowsAppSDK** — WinUI 3フレームワーク

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
