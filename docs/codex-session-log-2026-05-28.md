# Codex Session Log: KoMDViewer WPF

Date: 2026-05-28  
Workspace: `<local workspace>\KoMDViewerWPF`  
Repository target: `https://github.com/tikomosoftware/KoMDViewerWPF.git`

This document records the visible conversation and implementation flow from the Codex session that converted the project from an abandoned WinUI 3 attempt into a simple WPF Markdown viewer.

Notes:

- Codex cannot export private chain-of-thought. The "Codex judgment" entries below summarize the visible implementation reasoning, decisions, and actions taken during the session.
- User instructions were originally given largely by voice input. The quoted user instructions in this log are lightly normalized where speech recognition produced unclear text. They preserve the original intent rather than acting as a verbatim transcript.
- Local absolute paths and temporary machine-specific paths are redacted as `<local workspace>` or `<temp>`.

## Starting Context

User explained:

> これはWinUI 3で作成しようとして諦めたアプリです。単純に `.md` ファイルを閲覧するためだけのシンプルなツールを作りたかったのですが、WinUI 3ではうまく作れませんでした。その時はWPFで作ったらどうかと提案されました。

Codex response:

> 目的が「.md ファイルを開いて読むだけ」の軽量ビューアなら、WPFのほうが現実的。Markdown解析は Markdig、表示は WebView2 または FlowDocument が候補。

User:

> おねがいします

Codex judgment:

- Treat the request as implementation work, not just advice.
- Inspect project structure first.
- Preserve user work and avoid destructive operations.

Initial checks:

- `git status --short`
- `Get-ChildItem -Force`
- `rg --files`

Findings:

- Existing project still used WinUI 3 / Windows App SDK APIs.
- Files included `MainWindow.xaml`, `MainWindow.xaml.cs`, `App.xaml`, `App.xaml.cs`, `KoMDViewer.csproj`.
- `KoMDViewer.csproj` had `UseWinUI`, `Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Web.WebView2`, and `Markdig`.
- `MainWindow.xaml.cs` used `Microsoft.UI.Xaml`, `Windows.Storage.Pickers`, WinUI `MenuBar`, edit mode, PDF export, CodeMirror editor integration, recent files, and WebView2.

## Conversion To WPF

Codex judgment:

- Replace WinUI 3 app shell with WPF.
- Keep the scope simple: open Markdown/text files and display them.
- Use Markdig for Markdown-to-HTML.
- Use WPF WebView2 for rendering.
- Remove editor/PDF complexity from the active app code.
- Do not delete old directories yet.

Changed:

- `KoMDViewer.csproj`
  - `UseWPF` enabled.
  - `UseWinUI` removed.
  - Windows App SDK references removed.
  - Kept `Markdig` and `Microsoft.Web.WebView2`.
- `App.xaml`
  - Replaced WinUI application resources with WPF application resources.
- `App.xaml.cs`
  - Replaced WinUI `OnLaunched` with WPF `OnStartup`.
  - Preserved command-line file path open support.
- `MainWindow.xaml`
  - Replaced WinUI controls with WPF `Menu`, `StatusBar`, and `WebView2`.
  - Added open button and drag/drop overlay.
- `MainWindow.xaml.cs`
  - Replaced WinUI file picker with `Microsoft.Win32.OpenFileDialog`.
  - Added Markdown loading, HTML generation, recent files, drag/drop, dark theme.

Verification:

```powershell
dotnet build
```

Result:

```text
Build succeeded.
0 warnings
0 errors
```

## First Launch Check

User:

> 起動確認したいです

Codex action:

```powershell
Start-Process -FilePath "<local workspace>\bin\Debug\net9.0-windows\KoMDViewer.exe"
Get-Process | Where-Object { $_.ProcessName -eq 'KoMDViewer' }
```

Result:

- Process was running.
- A running `KoMDViewer` process was confirmed.

## Add Left History Pane

User:

> ペイン構成にして、左側にファイル履歴を表示したいです。履歴を選ぶと右側のMarkdown表示が切り替わる感じにしたいです。あとドラッグ&ドロップにも対応してほしいです。

Codex judgment:

- User wanted a two-pane viewer.
- Left pane should show recent file history.
- Selecting a history item should switch the right Markdown preview.
- Drag/drop should remain supported over the whole UI.

Changed:

- `MainWindow.xaml`
  - Added `Grid` with 3 columns:
    - left history pane
    - splitter
    - right Markdown preview
  - Added `RecentFilesList`.
  - Added `EmptyRecentFilesText`.
  - Added `GridSplitter`.
- `MainWindow.xaml.cs`
  - Added `RecentFileItem`.
  - Added `RecentFilesList_SelectionChanged`.
  - Synced recent files list with menu.
  - Selected current file in list.

Verification:

```powershell
dotnet build
Start-Process ...
Get-Process ...
```

Result:

- Build succeeded.
- App launched.

## Fix History Selection And Ordering

User:

> 左側のファイル履歴で選択を切り替えても、右側の表示が切り替わりません。あと、履歴で選択したものを一番上に持ってくるのはやめたいです。

Codex judgment:

- Selecting from history should not count as newly opening a file.
- Existing behavior moved selected file to top because `LoadMarkdownFileAsync` added it to recent files.
- WebView was loading a reused temporary HTML file path, which could cache or not visibly refresh.

Changed:

- History selection now calls:

```csharp
await LoadMarkdownFileAsync(item.FilePath, addToRecentFiles: false);
```

- Menu recent-file selection also uses `addToRecentFiles: false`.
- Removed preview file path approach.
- Switched to:

```csharp
MarkdownView.NavigateToString(html);
```

- Added WebView2 user data folder:

```csharp
MarkdownView.CreationProperties = new CoreWebView2CreationProperties
{
    UserDataFolder = WebViewDataDirectory
};
```

Reason:

- Avoid WebView2 access-denied/default profile issues.
- Ensure preview changes immediately when switching history items.

Verification:

- `dotnet build /p:UseAppHost=false`
- Direct launch via DLL initially exposed WebView2 `E_ACCESSDENIED`.
- After specifying `UserDataFolder`, DLL launch worked.
- Then normal build succeeded after closing the old app.

## Add History Context Menu, Pane Toggle, About Tech Stack

User:

> 履歴から削除するメニューを追加できませんか。履歴を選んで右クリックして削除、という感じがいいです。
> 履歴パネルの表示/非表示を切り替えるオプションが欲しいです。
> Aboutパネルには技術スタックなどを記載してほしいです。

Codex judgment:

- Add right-click delete from history.
- Do not delete actual files.
- Add View menu toggle for the history pane.
- Expand About text with tech stack.

Changed:

- `MainWindow.xaml`
  - Added `HistoryPaneMenuItem`.
  - Named `HistoryPane`, `HistorySplitter`, and their columns.
  - Added context menu to recent files.
- `MainWindow.xaml.cs`
  - Added `RemoveRecentFile_Click`.
  - Added `SetHistoryPaneVisible`.
  - Added tech stack to About.

Issue found:

- First context menu implementation inside `ListBoxItem` style caused WPF XAML connection error:

```text
System.InvalidCastException: Unable to cast object of type 'System.Windows.Controls.MenuItem' to type 'System.Windows.Controls.GridSplitter'.
```

Fix:

- Move context menu to `ListBox.ContextMenu`.
- Add `PreviewMouseRightButtonDown` to select the right-clicked item before showing the context menu.

Verification:

- `dotnet build` succeeded.
- App launched.

## Fix Drag Overlay Over WebView2

User supplied screenshot and asked:

> ドラッグしたときのエフェクトがずれている気がします。これは意図した表示ですか。

Codex judgment:

- Not intended.
- WebView2 is hosted in a way that can render above normal WPF overlays.
- Overlay was only visibly covering left pane while right WebView2 remained visible.

Changed:

- Added:

```csharp
private void ShowDropOverlay()
{
    MarkdownView.Visibility = Visibility.Hidden;
    DropOverlay.Visibility = Visibility.Visible;
}

private void HideDropOverlay()
{
    DropOverlay.Visibility = Visibility.Collapsed;
    MarkdownView.Visibility = Visibility.Visible;
}
```

- Drag enter/over calls `ShowDropOverlay`.
- Drag leave/drop calls `HideDropOverlay`.

User then asked:

> 右側のペインにドラッグしたときも、ドロップ表示が出るようにできますか。

Changed:

- Added drag/drop handlers directly to `MarkdownView`.
- Set:

```csharp
MarkdownView.AllowExternalDrop = false;
```

Reason:

- Ensure drag events are handled by the WPF app rather than WebView2 external drop behavior.

Verification:

- `dotnet build -o <temp>\KoMDViewer-build-check` succeeded.

## App Icon

User:

> アイコンを変えてほしいです。単純に「MD」という感じでもいいので、Markdownビューアらしいものを考えてくれませんか。

Codex judgment:

- For an app icon, deterministic vector-style asset is better than generated bitmap.
- Create a simple icon readable at taskbar size:
  - dark navy rounded tile
  - document shape
  - cyan `MD`
  - small downward chevron hinting Markdown/viewer
- Keep editable SVG source in repository.
- Generate multi-size `.ico` for WPF application icon.

Added:

- `Resources/app-icon.svg`

Updated:

- `Resources/app-icon.ico`

Preview was generated to:

```text
<temp>\komd-icon-preview.png
```

Verification:

- Preview image was inspected.
- `dotnet build -o <temp>\KoMDViewer-build-check` succeeded.

## Recent File Sorting Option

User:

> 履歴ファイルをアルファベット順に並べるオプションが欲しいです。並べ替えオプションのようなものです。

Codex judgment:

- Add toggle rather than permanently sorting.
- Preserve original recent-file order in storage.
- Display order should change in both left pane and menu.
- Persist user setting.

Changed:

- `MainWindow.xaml`
  - Added `SortRecentFilesMenuItem` under View menu.
- `MainWindow.xaml.cs`
  - Added `settings.json` under `%LocalAppData%\KoMDViewer`.
  - Added `AppSettings`.
  - Added `GetRecentFilesForDisplay`.
  - If enabled:

```csharp
files
    .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
    .ThenBy(path => path, StringComparer.CurrentCultureIgnoreCase)
```

Verification:

- `dotnet build -o <temp>\KoMDViewer-build-check` succeeded.

## Rename App

User:

> KoMDリーダーですが、WPF版なのでアプリ名を少し変えたいです。

Codex first judgment:

- Suggested `KoMDReader WPF`.
- Change display name and assembly name.
- Keep namespace/project file stable to avoid unnecessary churn.
- Preserve existing recent-file data path `%LocalAppData%\KoMDViewer`.

Changed temporarily:

- `AssemblyName` to `KoMDReaderWPF`.
- Product/title/About/README/package names to `KoMDReader WPF`.

User:

> `KoMDViewer WPF` にしませんか。ReaderよりViewerのほうが合っていると思います。

Codex judgment:

- Agree: Viewer is more accurate for the app.

Changed final name to:

- Display name: `KoMDViewer WPF`
- Assembly name/output executable: `KoMDViewerWPF.exe`
- Product: `KoMDViewer WPF`
- Manifest identity: `KoMDViewerWPF.app`
- README and packaging script names.

Verification:

```powershell
dotnet build -o <temp>\KoMDViewerWPF-build-check
Get-ChildItem <temp>\KoMDViewerWPF-build-check\KoMDViewerWPF.*
```

Output included:

- `KoMDViewerWPF.exe`
- `KoMDViewerWPF.dll`
- `KoMDViewerWPF.deps.json`
- `KoMDViewerWPF.runtimeconfig.json`

## About Dialog Icon

User:

> About画面にアイコンが表示されていますよね。これをアプリアイコンにしませんか。

Codex judgment:

- `MessageBox` uses generic system icons.
- Replace About with a small custom WPF dialog.
- Show application icon from `Resources/app-icon.ico`.

Changed:

- `About_Click` now creates a custom `Window`.
- Left side shows app icon.
- Right side shows app name, description, tech stack, and feature list.

Verification:

- `dotnet build -o <temp>\KoMDViewerWPF-build-check` succeeded.

## Repository Target And Push Preparation

User:

> [tikomosoftware/KoMDViewerWPF.git](https://github.com/tikomosoftware/KoMDViewerWPF.git)
>
> コミットしてpushしてください。push先はこのリポジトリに変更してください。

Codex judgment:

- Before commit/push, inspect status, branch, recent history, and remote.
- Follow push safety rules.
- Do not rewrite history.

Checks:

```powershell
git status --short --branch
git log --oneline --decorate -5
git remote -v
```

Findings:

- Branch: `main`
- `main` was aligned with `origin/main`
- Existing remote was:

```text
https://github.com/tikomosoftware/MDViewer.git
```

Changed remote:

```powershell
git remote set-url origin https://github.com/tikomosoftware/KoMDViewerWPF.git
```

Then user declined staging operation when `git add ...` was requested by the tool approval flow.

Status at that moment:

- No commit made.
- No push made.

## Pre-Commit Cleanup Review

User:

> その前に、以前のファイルが残っていたり、無駄なファイルがないか確認したいです。READMEなども今回のWPF版に合う内容に直っているか、全部確認したいです。

Codex judgment:

- Perform file inventory.
- Check README and scripts for stale WinUI/editor/Vite references.
- Do not delete files without explicit approval.

Checks:

```powershell
rg --files
rg "WinUI|WindowsAppSDK|Microsoft\.UI|CodeMirror|editor|vite|KoMDReader|KoMDViewer\.exe|KoMDViewer-v|MDViewer|KoMDViewer WPF|KoMDViewerWPF" ...
```

Findings:

- README still contained old WinUI 3, CodeMirror edit mode, PDF export, Windows App SDK Runtime, and unstable status text.
- `build-and-package.ps1` still referenced WinUI packaging concerns and old output path.
- `install-runtime.ps1` / `.bat` still checked Windows App Runtime.
- `editor/` and `frontend/` existed and were Git-tracked old Vite/editor assets.

Changed:

- Rewrote `README.md` for current WPF viewer.
- Rewrote `README_VECTOR.md` for current WPF viewer.
- Rewrote `install-runtime.ps1` and `install-runtime.bat` to check:
  - .NET 9 Desktop Runtime
  - WebView2 Runtime
- Removed Windows App SDK Runtime requirement from docs/scripts.
- Updated `build-and-package.ps1` output path:

```powershell
bin\Release\net9.0-windows\win-x64
```

- Removed stale WinUI packaging comments.

Verification:

```powershell
rg "editor|frontend|CodeMirror|vite|WindowsAppSDK|Microsoft\.UI"
dotnet build -o <temp>\KoMDViewerWPF-build-check
```

Result:

- Stale references remained only in `editor/` and `frontend/`.
- Build succeeded.

## Delete Old Editor And Frontend Assets

User:

> 不要なので削除してください

Codex judgment:

- Deleting Git-tracked directories is destructive enough to warrant explicit user approval.
- Approval was given.
- Create backup patch first.
- Delete only confirmed old directories:
  - `editor`
  - `frontend`

Backup created:

```powershell
git diff > <temp>\komdviewerwpf-before-cleanup.patch
```

Deleted:

- `editor/`
- `frontend/`

Verification:

```powershell
rg "editor|frontend|CodeMirror|vite|WindowsAppSDK|Microsoft\.UI"
dotnet build -o <temp>\KoMDViewerWPF-build-check
git status --short
```

Result:

- Old references removed.
- Build succeeded.
- Git status showed deletions for old tracked files.

## Current File State Summary

Main app files changed:

- `App.xaml`
- `App.xaml.cs`
- `KoMDViewer.csproj`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `app.manifest`

Docs/scripts changed:

- `README.md`
- `README_VECTOR.md`
- `build-and-package.ps1`
- `install-runtime.bat`
- `install-runtime.ps1`

Assets changed:

- `Resources/app-icon.ico`
- `Resources/app-icon.svg`

Deleted old assets:

- `editor/index.html`
- `editor/package-lock.json`
- `editor/package.json`
- `editor/src/main.js`
- `editor/src/style.css`
- `editor/vite.config.js`
- `frontend/.gitignore`
- `frontend/index.html`
- `frontend/package-lock.json`
- `frontend/package.json`
- `frontend/public/vite.svg`
- `frontend/src/counter.ts`
- `frontend/src/main.ts`
- `frontend/src/style.css`
- `frontend/src/typescript.svg`
- `frontend/tsconfig.json`

## Current App Behavior

Application name:

- `KoMDViewer WPF`

Output executable:

- `KoMDViewerWPF.exe`

Core behavior:

- Open Markdown/text files.
- Render Markdown through Markdig and WebView2.
- Support command-line file path.
- Support drag and drop.
- Show recent files in left pane.
- Allow recent file deletion from context menu.
- Allow recent file sorting by file name.
- Toggle history pane visibility.
- Toggle light/dark preview theme.
- Show custom About dialog with app icon and tech stack.

Current tech stack:

- .NET 9
- WPF
- Markdig
- Microsoft Edge WebView2
- System.Text.Json

Runtime requirements:

- .NET 9 Desktop Runtime
- Microsoft Edge WebView2 Runtime

Not part of current app anymore:

- WinUI 3
- Windows App SDK
- CodeMirror
- Vite frontend/editor
- PDF export
- edit mode

## Verification Commands Used

Representative commands:

```powershell
git status --short --branch
git log --oneline --decorate -5
git remote -v
dotnet build
dotnet build -o <temp>\KoMDViewerWPF-build-check
Start-Process -FilePath "<local workspace>\...\KoMDViewer.exe"
Get-Process | Where-Object { $_.ProcessName -eq 'KoMDViewer' }
rg --files
rg "editor|frontend|CodeMirror|vite|WindowsAppSDK|Microsoft\.UI"
```

Final verification after cleanup:

```text
dotnet build -o <temp>\KoMDViewerWPF-build-check
Build succeeded.
0 warnings
0 errors
```

## Push Status

Remote was changed to:

```text
https://github.com/tikomosoftware/KoMDViewerWPF.git
```

At the time this log was created:

- Changes were not committed yet.
- Changes were not pushed yet.
- Commit/push should be done after final review.
