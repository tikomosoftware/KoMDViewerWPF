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

### 配布バージョンの選び方

[Releases](../../releases)ページでは、用途に応じて2つの配布形態を提供しています：

#### 🪶 フレームワーク依存版（軽量）
- **ファイルサイズ**: 小さい
- **必要環境**: [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) のインストールが必要
- **メリット**: ダウンロードが高速

**ファイル名**: `KoMDViewer-v*-framework-dependent-release.zip`

#### 📦 自己完結型版（.NET同梱）
- **ファイルサイズ**: 大きい
- **必要環境**: なし（追加のインストールは一切不要）
- **メリット**: ダブルクリックするだけで確実に起動

**ファイル名**: `KoMDViewer-v*-standalone-release.zip`

### システム要件
- **OS**: Windows 10 version 1809 以降 / Windows 11
- **.NET**: .NET 9.0 Runtime（フレームワーク依存版のみ必要）
- **WebView2 Runtime**: Microsoft Edge WebView2（Windows 11は標準搭載、Windows 10は自動インストール）

### インストール手順

#### フレームワーク依存版
1. [Releases](../../releases)から`framework-dependent-release.zip`をダウンロード
2. ZIPファイルを展開
3. `KoMDViewer.exe` を実行

#### 自己完結型版
1. [Releases](../../releases)から`standalone-release.zip`をダウンロード
2. ZIPファイルを展開
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
- .NET 9.0 Runtimeがインストールされているか確認してください
- WebView2 Runtimeがインストールされているか確認してください

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
