using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace KoMDViewer
{
    public sealed partial class MainWindow : Window
    {
        private string currentFilePath = "";
        private string currentMarkdown = "";
        private string savedMarkdown = "";  // last saved content
        private bool isDark;
        private bool isEditMode;
        private bool hasUnsavedChanges;
        private readonly MarkdownPipeline mdPipeline;

        private static readonly string[] MarkdownExtensions =
            { ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".txt" };

        private static readonly string RecentFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KoMDViewer", "recent.json");

        private const int MaxRecentFiles = 10;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "KoMDViewer";

            mdPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            isDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
            UpdateThemeMenuText();

            try {
                this.ExtendsContentIntoTitleBar = true;
                this.SetTitleBar(AppTitleBar);
            } catch { }

            CenterOnScreen();
            SetWindowIcon();
            InitializeMica();
            RefreshRecentFilesMenu();
            _ = InitAsync();
        }

        private async System.Threading.Tasks.Task InitAsync()
        {
            await SetupWebView();

            // Open file from command line argument
            if (!string.IsNullOrEmpty(App.StartupFilePath))
                await LoadMarkdownFile(App.StartupFilePath);
        }

        private void CenterOnScreen()
        {
            try {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                int width = 820;
                int height = 680;
                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId,
                    Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                var workArea = displayArea.WorkArea;
                int x = (workArea.Width - width) / 2 + workArea.X;
                int y = (workArea.Height - height) / 2 + workArea.Y;
                appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            } catch { }
        }

        private void SetWindowIcon()
        {
            try {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");
                appWindow.SetIcon(iconPath);

                // カスタムタイトルバーにもアイコンを表示
                string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");
                if (File.Exists(pngPath))
                {
                    var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(pngPath));
                    TitleBarIcon.Source = bitmapImage;
                }
            } catch { }
        }

        private void InitializeMica()
        {
            try {
                if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            } catch { }
        }

        private async System.Threading.Tasks.Task SetupWebView()
        {
            try {
                await WebView.EnsureCoreWebView2Async();

                // Map editor dist folder for edit mode
                string editorDistPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "editor", "dist");
                if (Directory.Exists(editorDistPath))
                {
                    WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "editor.local", editorDistPath,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                }

                // Listen for messages from the editor
                WebView.CoreWebView2.WebMessageReceived += (sender, args) => {
                    try {
                        string json = args.WebMessageAsJson;
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        string type = root.GetProperty("type").GetString() ?? "";

                        if (type == "CONTENT_CHANGED")
                        {
                            if (root.TryGetProperty("content", out var content))
                                currentMarkdown = content.GetString() ?? "";
                            if (root.TryGetProperty("wordCount", out var wc))
                            {
                                int words = wc.GetInt32();
                                this.DispatcherQueue.TryEnqueue(() => {
                                    WordCountText.Text = $"{words} words";
                                    if (isEditMode)
                                    {
                                        hasUnsavedChanges = currentMarkdown != savedMarkdown;
                                        UpdateTitleForMode();
                                    }
                                });
                            }
                        }
                        else if (type == "CONTENT_RESPONSE")
                        {
                            if (root.TryGetProperty("content", out var content))
                                currentMarkdown = content.GetString() ?? "";
                        }
                        else if (type == "LOG")
                        {
                            if (root.TryGetProperty("message", out var logMsg))
                                System.Diagnostics.Debug.WriteLine($"[JS] {logMsg.GetString()}");
                        }
                        else if (type == "SAVE_REQUEST")
                        {
                            this.DispatcherQueue.TryEnqueue(async () => await SaveCurrentFile());
                        }
                        else if (type == "EXIT_EDIT_MODE")
                        {
                            this.DispatcherQueue.TryEnqueue(async () => {
                                if (isEditMode) await ExitEditMode();
                            });
                        }
                    } catch { }
                };
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"WebView setup error: {ex}");
            }
        }

        // ===== Theme =====

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDark = !isDark;
            UpdateThemeMenuText();

            if (Content is FrameworkElement root)
                root.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

            if (!isEditMode && !string.IsNullOrEmpty(currentMarkdown))
                WebView.NavigateToString(BuildHtml(currentMarkdown));
        }

        private void UpdateThemeMenuText()
        {
            ThemeToggleItem.Text = isDark ? "ライトモードに切り替え" : "ダークモードに切り替え";
        }

        // ===== Recent Files =====

        private List<string> LoadRecentFiles()
        {
            try {
                if (File.Exists(RecentFilesPath))
                {
                    string json = File.ReadAllText(RecentFilesPath);
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            } catch { }
            return new List<string>();
        }

        private void SaveRecentFiles(List<string> files)
        {
            try {
                string dir = Path.GetDirectoryName(RecentFilesPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(RecentFilesPath, JsonSerializer.Serialize(files));
            } catch { }
        }

        private void AddToRecentFiles(string filePath)
        {
            var files = LoadRecentFiles();
            files.Remove(filePath);
            files.Insert(0, filePath);
            if (files.Count > MaxRecentFiles)
                files = files.Take(MaxRecentFiles).ToList();
            SaveRecentFiles(files);
            RefreshRecentFilesMenu();
        }

        private void RefreshRecentFilesMenu()
        {
            RecentFilesMenu.Items.Clear();
            var files = LoadRecentFiles();

            if (files.Count == 0)
            {
                var empty = new MenuFlyoutItem { Text = "(なし)", IsEnabled = false };
                RecentFilesMenu.Items.Add(empty);
                return;
            }

            foreach (var filePath in files)
            {
                var item = new MenuFlyoutItem { Text = Path.GetFileName(filePath) };
                string captured = filePath;
                item.Click += async (s, e) => await LoadMarkdownFile(captured);
                RecentFilesMenu.Items.Add(item);
            }

            RecentFilesMenu.Items.Add(new MenuFlyoutSeparator());
            var clearItem = new MenuFlyoutItem { Text = "履歴をクリア" };
            clearItem.Click += (s, e) => {
                SaveRecentFiles(new List<string>());
                RefreshRecentFilesMenu();
            };
            RecentFilesMenu.Items.Add(clearItem);
        }

        // ===== Markdown → HTML =====

        private string BuildHtml(string markdown, bool forPdf = false)
        {
            string bodyHtml = Markdig.Markdown.ToHtml(markdown, mdPipeline);
            string css;
            string hljsTheme;

            if (forPdf)
            {
                // PDF always uses light theme with solid background
                css = GetLightCss().Replace("rgba(255,255,255,0.6)", "#ffffff");
                hljsTheme = "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github.min.css";
            }
            else
            {
                css = isDark ? GetDarkCss() : GetLightCss();
                hljsTheme = isDark
                    ? "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github-dark.min.css"
                    : "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github.min.css";
            }

            return $@"<!DOCTYPE html>
<html lang=""ja"">
<head>
<meta charset=""UTF-8"">
<link rel=""stylesheet"" href=""{hljsTheme}"">
<style>
{GetBaseCss()}
{css}
</style>
</head>
<body>
<article>{bodyHtml}</article>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/highlight.min.js""></script>
<script>hljs.highlightAll();</script>
</body>
</html>";
        }

        private static string GetBaseCss()
        {
            return @"
* { box-sizing: border-box; }
body {
    font-family: 'Segoe UI', 'Yu Gothic UI', 'Meiryo', sans-serif;
    line-height: 1.8;
    background: transparent;
    max-width: 100%;
    margin: 0 auto;
    padding: 32px 40px;
}
h1 { font-size: 2em; padding-bottom: 0.3em; margin-top: 1.5em; }
h2 { font-size: 1.5em; padding-bottom: 0.3em; margin-top: 1.5em; }
h3 { font-size: 1.25em; margin-top: 1.5em; }
h1:first-child, h2:first-child, h3:first-child { margin-top: 0; }
a { text-decoration: none; }
a:hover { text-decoration: underline; }
code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    padding: 0.2em 0.4em;
    border-radius: 4px;
    font-size: 0.9em;
}
pre {
    padding: 16px;
    border-radius: 6px;
    overflow-x: auto;
    line-height: 1.5;
}
pre code { background: none; padding: 0; border-radius: 6px; }
pre:has(code.hljs) { padding: 0; background: none; }
blockquote {
    margin: 1em 0;
    padding: 0.5em 1em;
}
table { border-collapse: collapse; width: 100%; margin: 1em 0; }
th, td { padding: 8px 12px; text-align: left; }
th { font-weight: 600; }
img { max-width: 100%; height: auto; }
hr { border: none; margin: 2em 0; }
ul, ol { padding-left: 2em; }
li { margin: 0.25em 0; }
input[type='checkbox'] { margin-right: 0.5em; }
";
        }

        private static string GetLightCss()
        {
            return @"
body { color: #24292f; background: rgba(255,255,255,0.6); }
h1, h2 { border-bottom: 1px solid #d0d7de; }
a { color: #0969da; }
code { background: rgba(0,0,0,0.05); }
pre { background: rgba(0,0,0,0.04); }
blockquote { border-left: 4px solid #d0d7de; color: #57606a; }
th, td { border: 1px solid #d0d7de; }
th { background: rgba(0,0,0,0.03); }
hr { border-top: 1px solid #d0d7de; }
";
        }

        private static string GetDarkCss()
        {
            return @"
body { color: #c9d1d9; background: rgba(30,30,30,0.6); }
h1, h2 { border-bottom: 1px solid #444c56; }
a { color: #58a6ff; }
code { background: rgba(255,255,255,0.1); }
pre { background: rgba(255,255,255,0.07); }
blockquote { border-left: 4px solid #444c56; color: #8b949e; }
th, td { border: 1px solid #444c56; }
th { background: rgba(255,255,255,0.05); }
hr { border-top: 1px solid #444c56; }
";
        }

        // ===== File Open (Menu) =====

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".md");
                picker.FileTypeFilter.Add(".markdown");
                picker.FileTypeFilter.Add(".txt");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                    await LoadMarkdownFile(file.Path);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OpenFile error: {ex}");
            }
        }

        // ===== PDF Export =====

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentMarkdown))
                return;

            try {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("PDF", new List<string> { ".pdf" });
                picker.SuggestedFileName = string.IsNullOrEmpty(currentFilePath)
                    ? "document"
                    : Path.GetFileNameWithoutExtension(currentFilePath);

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                StorageFile file = await picker.PickSaveFileAsync();
                if (file == null) return;

                LoadingBar.Visibility = Visibility.Visible;
                await System.Threading.Tasks.Task.Delay(30);

                // Render light-theme HTML for PDF
                string pdfHtml = BuildHtml(currentMarkdown, forPdf: true);

                // Navigate to the PDF HTML, wait for it to load
                var navTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                void navHandler(WebView2 s, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs a)
                {
                    WebView.NavigationCompleted -= navHandler;
                    navTcs.TrySetResult(true);
                }
                WebView.NavigationCompleted += navHandler;
                WebView.NavigateToString(pdfHtml);
                await navTcs.Task;

                // Wait a moment for highlight.js to finish
                await System.Threading.Tasks.Task.Delay(500);

                // Print to PDF
                var printSettings = WebView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.ShouldPrintBackgrounds = true;
                await WebView.CoreWebView2.PrintToPdfAsync(file.Path, printSettings);

                // Restore the current theme view
                WebView.NavigateToString(BuildHtml(currentMarkdown));

                LoadingBar.Visibility = Visibility.Collapsed;
            } catch (Exception ex) {
                LoadingBar.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"ExportPdf error: {ex}");
            }
        }

        // ===== Drag & Drop =====

        private void DropTarget_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "開く";
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsGlyphVisible = true;
                DropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void DropTarget_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void DropTarget_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var items = await e.DataView.GetStorageItemsAsync();
            var file = items.OfType<StorageFile>().FirstOrDefault(f =>
                MarkdownExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()));

            if (file != null)
                await LoadMarkdownFile(file.Path);
        }

        // ===== Load & Display =====

        private async System.Threading.Tasks.Task LoadMarkdownFile(string filePath)
        {
            try {
                if (!File.Exists(filePath))
                {
                    var dialog = new ContentDialog
                    {
                        Title = "ファイルが見つかりません",
                        Content = $"以下のファイルが存在しません。移動または削除された可能性があります。\n\n{filePath}",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                    };
                    await dialog.ShowAsync();

                    // 履歴から削除して更新
                    var recentFiles = LoadRecentFiles();
                    recentFiles.Remove(filePath);
                    SaveRecentFiles(recentFiles);
                    RefreshRecentFilesMenu();
                    return;
                }

                LoadingBar.Visibility = Visibility.Visible;
                await System.Threading.Tasks.Task.Delay(30);

                currentMarkdown = await File.ReadAllTextAsync(filePath);
                currentFilePath = filePath;
                savedMarkdown = currentMarkdown;
                hasUnsavedChanges = false;

                string html = BuildHtml(currentMarkdown);

                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                void handler(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
                {
                    WebView.NavigationCompleted -= handler;
                    tcs.TrySetResult(true);
                }
                WebView.NavigationCompleted += handler;
                WebView.NavigateToString(html);
                await tcs.Task;

                string fileName = Path.GetFileName(filePath);
                this.Title = $"{fileName} - KoMDViewer";
                TitleText.Text = $"{fileName} - KoMDViewer";
                FilePathText.Text = filePath;

                int words = currentMarkdown.Split(new[] { ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries).Length;
                WordCountText.Text = $"{words} words";

                AddToRecentFiles(filePath);
                EditModeItem.IsEnabled = true;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"LoadMarkdownFile error: {ex}");
            } finally {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }
        // ===== Edit Mode =====

        private async void ToggleEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath)) return;

            if (!isEditMode)
            {
                await EnterEditMode();
            }
            else
            {
                await ExitEditMode();
            }
        }

        private async System.Threading.Tasks.Task EnterEditMode()
        {
            isEditMode = true;
            EditModeItem.Text = "表示モードに戻る (Esc)";
            SaveMenuItem.IsEnabled = true;

            // Navigate to CodeMirror editor
            var navTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            void navHandler(WebView2 s, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs a)
            {
                WebView.NavigationCompleted -= navHandler;
                navTcs.TrySetResult(true);
            }
            WebView.NavigationCompleted += navHandler;
            WebView.Source = new Uri("https://editor.local/index.html");
            await navTcs.Task;

            // Wait for editor to initialize, then send raw markdown
            await System.Threading.Tasks.Task.Delay(300);
            var msg = JsonSerializer.Serialize(new { type = "SET_CONTENT", content = currentMarkdown, dark = isDark });
            WebView.CoreWebView2.PostWebMessageAsJson(msg);

            UpdateTitleForMode();
        }

        private async System.Threading.Tasks.Task ExitEditMode()
        {
            // Get latest content directly from CodeMirror
            await SyncContentFromEditor();

            isEditMode = false;
            EditModeItem.Text = "編集モード";
            SaveMenuItem.IsEnabled = false;

            // Switch back to Markdig view
            WebView.NavigateToString(BuildHtml(currentMarkdown));
            UpdateTitleForMode();
        }

        private async System.Threading.Tasks.Task SyncContentFromEditor()
        {
            try {
                string result = await WebView.CoreWebView2.ExecuteScriptAsync("window.__komdGetContent()");
                // Result is a JSON-encoded string
                string content = System.Text.Json.JsonSerializer.Deserialize<string>(result) ?? "";
                if (!string.IsNullOrEmpty(content))
                    currentMarkdown = content;
            } catch { }
        }

        private void UpdateTitleForMode()
        {
            string fileName = Path.GetFileName(currentFilePath);
            string unsaved = hasUnsavedChanges ? "● " : "";
            string mode = isEditMode ? " [編集]" : "";
            this.Title = $"{unsaved}{fileName}{mode} - KoMDViewer";
            TitleText.Text = $"{unsaved}{fileName}{mode} - KoMDViewer";
        }

        // ===== Save =====

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            await SaveCurrentFile();
        }

        private async System.Threading.Tasks.Task SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(currentFilePath) || !isEditMode) return;

            try {
                await SyncContentFromEditor();
                await File.WriteAllTextAsync(currentFilePath, currentMarkdown);
                savedMarkdown = currentMarkdown;
                hasUnsavedChanges = false;
                UpdateTitleForMode();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Save error: {ex}");
            }
        }

        // ===== About =====

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "KoMDViewer",
                CloseButtonText = "閉じる",
                XamlRoot = this.Content.XamlRoot,
                Content = new ScrollViewer
                {
                    MaxHeight = 400,
                    Content = new TextBlock
                    {
                        Text = "KoMDViewer v1.0.0\n" +
                               "Markdown Viewer & Editor\n\n" +
                               "━━━ 使用ライブラリ ━━━\n\n" +
                               "■ Markdig v0.40.0\n" +
                               "  License: BSD-2-Clause\n" +
                               "  Copyright (c) Alexandre Mutel\n" +
                               "  https://github.com/xoofx/markdig\n\n" +
                               "■ CodeMirror 6\n" +
                               "  License: MIT\n" +
                               "  Copyright (c) Marijn Haverbeke and others\n" +
                               "  https://codemirror.net/\n\n" +
                               "■ highlight.js v11.11.1\n" +
                               "  License: BSD-3-Clause\n" +
                               "  Copyright (c) Ivan Sagalaev\n" +
                               "  https://highlightjs.org/\n\n" +
                               "■ Microsoft.Web.WebView2\n" +
                               "  License: Microsoft Software License\n" +
                               "  https://www.nuget.org/packages/Microsoft.Web.WebView2\n\n" +
                               "■ Microsoft.WindowsAppSDK\n" +
                               "  License: Microsoft Software License\n" +
                               "  https://www.nuget.org/packages/Microsoft.WindowsAppSDK",
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                        IsTextSelectionEnabled = true,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
                        FontSize = 13,
                        LineHeight = 20,
                    }
                }
            };
            await dialog.ShowAsync();
        }
    }
}
