# KoMDViewer WPF

Windows向けのシンプルなMarkdownビューアです。WPF、Markdig、WebView2で構成されています。

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- Markdown / text file viewer
- Left history pane
- Recent-file removal from the history context menu
- Recent-file sorting by file name
- Drag and drop support
- Light / dark preview theme
- Command-line file open

## Requirements

- Windows 10 version 1809 or later / Windows 11
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

Windows 11 normally includes WebView2. On Windows 10, install it if the preview pane does not appear.

## Install

1. Download `KoMDViewerWPF-v*-release.zip` from Releases.
2. Extract the ZIP to any folder.
3. Run `install-runtime.bat` if this is the first time using the app on the PC.
4. Run `KoMDViewerWPF.exe`.

## Usage

- Open a file: `File > Open...` or `Ctrl + O`
- Drag and drop: drop a `.md`, `.markdown`, or `.txt` file onto the window
- Open from command line:

```powershell
KoMDViewerWPF.exe "C:\path\to\file.md"
```

## Supported Files

- `.md`
- `.markdown`
- `.mdown`
- `.mkd`
- `.mkdn`
- `.txt`

## Development

```powershell
dotnet restore
dotnet build
dotnet run
```

Release package:

```powershell
.\build-and-package.ps1 -Version "1.1"
```

## Tech Stack

- .NET 9
- WPF
- Markdig
- Microsoft Edge WebView2
- System.Text.Json

## Notes

Recent files and settings are stored under:

```text
%LocalAppData%\KoMDViewer
```

The internal namespace remains `KoMDViewer` for compatibility with the existing project structure.

## Support

- E-Mail: tikomo@gmail.com
- HP: https://tikomosoftware.github.io

© 2026 tikomo software
