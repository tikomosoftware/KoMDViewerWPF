using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Markdig;
using Microsoft.Win32;

namespace KoMDViewer;

public partial class MainWindow : Window
{
    private const string AppDisplayName = "KoMDViewer WPF";
    private const int MaxRecentFiles = 10;

    private static readonly string[] MarkdownExtensions =
    [
        ".md",
        ".markdown",
        ".mdown",
        ".mkd",
        ".mkdn",
        ".txt"
    ];

    private static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KoMDViewer");

    private static readonly string WebViewDataDirectory = Path.Combine(AppDataDirectory, "WebView2");

    private static readonly string RecentFilesPath = Path.Combine(AppDataDirectory, "recent.json");
    private static readonly string SettingsPath = Path.Combine(AppDataDirectory, "settings.json");
    private readonly MarkdownPipeline markdownPipeline;
    private string currentFilePath = string.Empty;
    private bool isRefreshingRecentFiles;
    private bool sortRecentFilesAlphabetically;
    private GridLength historyPaneWidth = new(260);

    public MainWindow()
    {
        InitializeComponent();

        markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        MarkdownView.CreationProperties = new Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties
        {
            UserDataFolder = WebViewDataDirectory
        };
        MarkdownView.AllowExternalDrop = false;

        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenFileCommand_Executed));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Open, Key.O, ModifierKeys.Control));

        var settings = LoadSettings();
        sortRecentFilesAlphabetically = settings.SortRecentFilesAlphabetically;
        SortRecentFilesMenuItem.IsChecked = sortRecentFilesAlphabetically;

        RefreshRecentFilesMenu();

        Loaded += async (_, _) =>
        {
            await MarkdownView.EnsureCoreWebView2Async();
            ShowEmptyPage();

            if (!string.IsNullOrEmpty(App.StartupFilePath))
            {
                await LoadMarkdownFileAsync(App.StartupFilePath);
            }
        };
    }

    private void OpenFileCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenFile_Click(sender, e);
    }

    private async void RecentFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isRefreshingRecentFiles || RecentFilesList.SelectedItem is not RecentFileItem item)
        {
            return;
        }

        await LoadMarkdownFileAsync(item.FilePath, addToRecentFiles: false);
    }

    private void RecentFilesList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item is not null)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }

    private void RemoveRecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (RecentFilesList.SelectedItem is not RecentFileItem item)
        {
            return;
        }

        RemoveFromRecentFiles(item.FilePath);
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Markdownファイルを開く",
            Filter = "Markdownファイル (*.md;*.markdown;*.mdown;*.mkd;*.mkdn)|*.md;*.markdown;*.mdown;*.mkd;*.mkdn|テキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            await LoadMarkdownFileAsync(dialog.FileName);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentFilePath))
        {
            await LoadMarkdownFileAsync(currentFilePath, addToRecentFiles: false);
        }
        else
        {
            ShowEmptyPage();
        }
    }

    private void HistoryPaneMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SetHistoryPaneVisible(HistoryPaneMenuItem.IsChecked);
    }

    private void SortRecentFilesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        sortRecentFilesAlphabetically = SortRecentFilesMenuItem.IsChecked;
        SaveSettings(new AppSettings
        {
            SortRecentFilesAlphabetically = sortRecentFilesAlphabetically
        });
        RefreshRecentFilesMenu();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-icon.ico");
        var icon = File.Exists(iconPath)
            ? new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath))
            : null;

        var closeButton = new Button
        {
            Content = "OK",
            Width = 88,
            MinHeight = 28,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true,
            IsCancel = true
        };

        var dialog = new Window
        {
            Title = $"{AppDisplayName}について",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            Icon = Icon,
            Content = new Grid
            {
                Margin = new Thickness(22),
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = new GridLength(24) },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Auto },
                            new RowDefinition { Height = new GridLength(18) },
                            new RowDefinition { Height = GridLength.Auto }
                        },
                        Children =
                        {
                            new Image
                            {
                                Source = icon,
                                Width = 64,
                                Height = 64,
                                VerticalAlignment = VerticalAlignment.Top
                            },
                            new TextBlock
                            {
                                Text = $"{AppDisplayName}\n\n" +
                                       "シンプルなMarkdown閲覧ツールです。\n\n" +
                                       "技術スタック\n" +
                                       "- .NET 9\n" +
                                       "- WPF\n" +
                                       "- Markdig: MarkdownをHTMLへ変換\n" +
                                       "- Microsoft Edge WebView2: HTMLプレビュー表示\n" +
                                       "- System.Text.Json: 最近開いたファイル履歴の保存\n\n" +
                                       "主な機能\n" +
                                       "- Markdown / テキストファイルの表示\n" +
                                       "- ドラッグ＆ドロップで開く\n" +
                                       "- 左側履歴ペイン\n" +
                                       "- ダークモード",
                                Width = 420,
                                TextWrapping = TextWrapping.Wrap
                            },
                            closeButton
                        }
                    }
                }
            }
        };

        Grid.SetColumn((UIElement)((Grid)((Grid)dialog.Content).Children[0]).Children[1], 2);
        Grid.SetRow(closeButton, 2);
        Grid.SetColumn(closeButton, 2);
        closeButton.Click += (_, _) => dialog.Close();
        dialog.ShowDialog();
    }

    private void DropTarget_DragEnter(object sender, DragEventArgs e)
    {
        UpdateDropState(e);
    }

    private void DropTarget_DragOver(object sender, DragEventArgs e)
    {
        UpdateDropState(e);
    }

    private void DropTarget_DragLeave(object sender, DragEventArgs e)
    {
        HideDropOverlay();
    }

    private async void DropTarget_Drop(object sender, DragEventArgs e)
    {
        HideDropOverlay();

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        var filePath = files?.FirstOrDefault(IsMarkdownFile);
        if (!string.IsNullOrEmpty(filePath))
        {
            await LoadMarkdownFileAsync(filePath);
        }
    }

    private void UpdateDropState(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files?.Any(IsMarkdownFile) == true)
            {
                e.Effects = DragDropEffects.Copy;
                ShowDropOverlay();
                e.Handled = true;
                return;
            }
        }

        e.Effects = DragDropEffects.None;
        HideDropOverlay();
        e.Handled = true;
    }

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

    private async Task LoadMarkdownFileAsync(string filePath, bool addToRecentFiles = true)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show(this, $"ファイルが見つかりません:\n\n{filePath}", "ファイルが見つかりません", MessageBoxButton.OK, MessageBoxImage.Warning);
            RemoveFromRecentFiles(filePath);
            return;
        }

        if (!IsMarkdownFile(filePath))
        {
            MessageBox.Show(this, "Markdownまたはテキストファイルを選んでください。", "対応していないファイル", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var markdown = await File.ReadAllTextAsync(filePath);
            var html = BuildHtml(markdown, filePath);

            Directory.CreateDirectory(AppDataDirectory);
            await MarkdownView.EnsureCoreWebView2Async();

            currentFilePath = filePath;
            MarkdownView.NavigateToString(html);
            WelcomePanel.Visibility = Visibility.Collapsed;

            var fileName = Path.GetFileName(filePath);
            Title = $"{fileName} - {AppDisplayName}";
            FilePathText.Text = filePath;
            WordCountText.Text = $"{CountWords(markdown):N0} words";

            if (addToRecentFiles)
            {
                AddToRecentFiles(filePath);
            }
            else
            {
                SelectRecentFile(filePath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "ファイルを開けませんでした", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string BuildHtml(string markdown, string filePath)
    {
        var bodyHtml = Markdig.Markdown.ToHtml(markdown, markdownPipeline);
        var baseUri = new Uri(Path.GetDirectoryName(filePath)! + Path.DirectorySeparatorChar).AbsoluteUri;
        var themeCss = ThemeMenuItem.IsChecked
            ? """
              body { color: #d6deeb; background: #111827; }
              article { background: #111827; }
              h1, h2 { border-bottom-color: #374151; }
              a { color: #7dd3fc; }
              code { background: #1f2937; }
              pre { background: #0f172a; }
              blockquote { border-left-color: #4b5563; color: #a7b0bf; }
              th, td { border-color: #374151; }
              th { background: #1f2937; }
              hr { border-top-color: #374151; }
              """
            : """
              body { color: #24292f; background: #ffffff; }
              article { background: #ffffff; }
              h1, h2 { border-bottom-color: #d0d7de; }
              a { color: #0969da; }
              code { background: rgba(0,0,0,0.06); }
              pre { background: #f6f8fa; }
              blockquote { border-left-color: #d0d7de; color: #57606a; }
              th, td { border-color: #d0d7de; }
              th { background: #f6f8fa; }
              hr { border-top-color: #d0d7de; }
              """;

        return $$"""
            <!doctype html>
            <html lang="ja">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <base href="{{baseUri}}">
              <style>
                * { box-sizing: border-box; }
                body {
                  margin: 0;
                  font-family: "Segoe UI", "Yu Gothic UI", Meiryo, sans-serif;
                  line-height: 1.75;
                }
                article {
                  max-width: 920px;
                  margin: 0 auto;
                  padding: 36px 44px 64px;
                }
                h1 { font-size: 2rem; margin: 0 0 0.75em; padding-bottom: 0.3em; border-bottom: 1px solid; }
                h2 { font-size: 1.5rem; margin-top: 1.6em; padding-bottom: 0.3em; border-bottom: 1px solid; }
                h3 { font-size: 1.25rem; margin-top: 1.4em; }
                a { text-decoration: none; }
                a:hover { text-decoration: underline; }
                code {
                  font-family: "Cascadia Code", Consolas, monospace;
                  padding: 0.15em 0.35em;
                  border-radius: 4px;
                  font-size: 0.9em;
                }
                pre {
                  padding: 16px;
                  border-radius: 6px;
                  overflow-x: auto;
                  line-height: 1.5;
                }
                pre code { background: transparent; padding: 0; }
                blockquote { margin: 1em 0; padding: 0.4em 1em; border-left: 4px solid; }
                table { border-collapse: collapse; width: 100%; margin: 1em 0; }
                th, td { border: 1px solid; padding: 8px 12px; text-align: left; }
                th { font-weight: 600; }
                img { max-width: 100%; height: auto; }
                hr { border: 0; border-top: 1px solid; margin: 2em 0; }
                ul, ol { padding-left: 2em; }
                li { margin: 0.25em 0; }
                {{themeCss}}
              </style>
            </head>
            <body>
              <article>{{bodyHtml}}</article>
            </body>
            </html>
            """;
    }

    private void ShowEmptyPage()
    {
        var background = ThemeMenuItem.IsChecked ? "#111827" : "#ffffff";
        MarkdownView.NavigateToString($"""
            <!doctype html>
            <html>
            <body style="margin:0;background:{background};"></body>
            </html>
            """);
    }

    private void RefreshRecentFilesMenu()
    {
        var recentFiles = GetRecentFilesForDisplay();

        RecentFilesMenu.Items.Clear();
        RefreshRecentFilesList(recentFiles);

        if (recentFiles.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem { Header = "(なし)", IsEnabled = false });
            return;
        }

        foreach (var filePath in recentFiles)
        {
            var item = new MenuItem
            {
                Header = Path.GetFileName(filePath),
                ToolTip = filePath
            };
            item.Click += async (_, _) => await LoadMarkdownFileAsync(filePath, addToRecentFiles: false);
            RecentFilesMenu.Items.Add(item);
        }

        RecentFilesMenu.Items.Add(new Separator());

        var clearItem = new MenuItem { Header = "履歴をクリア" };
        clearItem.Click += (_, _) =>
        {
            SaveRecentFiles([]);
            RefreshRecentFilesMenu();
        };
        RecentFilesMenu.Items.Add(clearItem);
    }

    private static List<string> LoadRecentFiles()
    {
        try
        {
            if (File.Exists(RecentFilesPath))
            {
                return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(RecentFilesPath)) ?? [];
            }
        }
        catch
        {
        }

        return [];
    }

    private List<string> GetRecentFilesForDisplay()
    {
        var files = LoadRecentFiles();
        return sortRecentFilesAlphabetically
            ? files
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(path => path, StringComparer.CurrentCultureIgnoreCase)
                .ToList()
            : files;
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            }
        }
        catch
        {
        }

        return new AppSettings();
    }

    private static void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
    }

    private void AddToRecentFiles(string filePath)
    {
        var files = LoadRecentFiles();
        files.RemoveAll(path => string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase));
        files.Insert(0, filePath);
        SaveRecentFiles(files.Take(MaxRecentFiles).ToList());
        RefreshRecentFilesMenu();
        SelectRecentFile(filePath);
    }

    private void RemoveFromRecentFiles(string filePath)
    {
        var files = LoadRecentFiles();
        files.RemoveAll(path => string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase));
        SaveRecentFiles(files);
        RefreshRecentFilesMenu();
    }

    private void RefreshRecentFilesList(List<string> recentFiles)
    {
        try
        {
            isRefreshingRecentFiles = true;
            RecentFilesList.ItemsSource = recentFiles
                .Select(path => new RecentFileItem(path))
                .ToList();

            var hasItems = recentFiles.Count > 0;
            RecentFilesList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            EmptyRecentFilesText.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            SelectRecentFile(currentFilePath);
        }
        finally
        {
            isRefreshingRecentFiles = false;
        }
    }

    private void SetHistoryPaneVisible(bool isVisible)
    {
        if (!isVisible && HistoryPaneColumn.Width.Value > 0)
        {
            historyPaneWidth = HistoryPaneColumn.Width;
        }

        HistoryPane.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        HistorySplitter.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        HistoryPaneColumn.Width = isVisible ? historyPaneWidth : new GridLength(0);
        HistoryPaneColumn.MinWidth = isVisible ? 180 : 0;
        HistorySplitterColumn.Width = isVisible ? new GridLength(5) : new GridLength(0);
    }

    private void SelectRecentFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            RecentFilesList.SelectedItem = null;
            return;
        }

        foreach (var item in RecentFilesList.Items.OfType<RecentFileItem>())
        {
            if (string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                RecentFilesList.SelectedItem = item;
                RecentFilesList.ScrollIntoView(item);
                return;
            }
        }
    }

    private static void SaveRecentFiles(List<string> files)
    {
        Directory.CreateDirectory(AppDataDirectory);
        File.WriteAllText(RecentFilesPath, JsonSerializer.Serialize(files));
    }

    private static bool IsMarkdownFile(string filePath)
    {
        return MarkdownExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
    }

    private static int CountWords(string text)
    {
        return text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
            {
                return parent;
            }

            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private sealed class RecentFileItem(string filePath)
    {
        public string FilePath { get; } = filePath;

        public string DisplayName { get; } = Path.GetFileName(filePath);
    }

    private sealed class AppSettings
    {
        public bool SortRecentFilesAlphabetically { get; set; }
    }
}
