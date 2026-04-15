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
        private bool isDark;
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

            if (!string.IsNullOrEmpty(currentMarkdown))
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
    max-width: 860px;
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
                LoadingBar.Visibility = Visibility.Visible;
                await System.Threading.Tasks.Task.Delay(30);

                currentMarkdown = await File.ReadAllTextAsync(filePath);
                currentFilePath = filePath;

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
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"LoadMarkdownFile error: {ex}");
            } finally {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }
    }
}
