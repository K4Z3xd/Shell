[![Ceasefire Now](https://badge.techforpalestine.org/default)](https://techforpalestine.org/learn-more)

[![Build](../../actions/workflows/build.yml/badge.svg)](../../actions/workflows/build.yml)
[![Nightly](https://img.shields.io/badge/Nightly-nightly.link-purple)](https://nightly.link/moudey/Shell/workflows/build/main)

# [Shell](https://nilesoft.org)
Powerful manager for Windows File Explorer context menu.
<br>

<p align="center">
 <img src="https://www.nilesoft.org/images/logo-256.png">
 <br>
 <br>
</p>

## Details
<p>
Shell is a context menu extender that lets you handpick the items to integrate into the Windows File Explorer context menu, create custom commands to access all your favorite web pages, files, and folders, and launch any application directly from the context menu.<br>
It also provides you a convenient solution to modify or remove any context menu item added by the system or third-party software.
</p>

Features
------------------
* Lightweight, portable, and relatively easy to use.
* Fully customize the appearance.
* Adding new custom items such as (sub-menu, menu-items, and separator).
* Modify or remove items that already exist.
* Support all file system objects, including files, folders, desktop, and the taskbar.
* Support expressions syntax. with built-in functions and predefined variables.
* Support colors, glyphs, SVG, embedded icons, and image files such as .ico, .png or .bmp.
* Support search and filter.
* Support for complex nested menus.
* Support multiple columns.
* Quickly and easily configure file in plain text.
* Minimal resource usage.
* No limitations.

## Acrylic GUI Configurator (`ShellConfigurator`)
This fork includes a native Windows **.NET 8 WPF** GUI (`src/gui/Shell.Configurator`) featuring a **Dark / Light Windows Acrylic** backdrop to customize every context menu category without editing `.nss` scripts manually:

* **General & Modules** (`shell.nss`) — show delay, hook priority, localization language, and master toggles for every category.
* **Theme & Acrylic** (`imports/theme.nss`) — live preview, preset, dark mode, density, Acrylic/Blur/Transparent effects, border radius, shadow, and custom font.
* **Modify System Items** (`imports/modify.nss`) — hide unwanted Windows entries or move items into `More options` / `Pin/Unpin`.
* **Terminal** (`imports/terminal.nss`) — Command Prompt, PowerShell, Windows Terminal, and Shift/Right-Click admin elevation.
* **File Manage** (`imports/file-manage.nss`) — Copy Path options, Change Extension, Select, Take Ownership, Show/Hide, Attributes, Register Server, New File/Folder templates, and Folder Options.
* **Develop** (`imports/develop.nss`) — Editors (VS Code, Notepad), `dotnet` CLI presets, and configurable paths for external tools.
* **Go To** (`imports/goto.nss`) — System/User folders, Control Panel, Run, and Windows Settings shortcuts.
* **Taskbar** (`imports/taskbar.nss`) — Quick Apps, Window arrangement, Task Manager, Shell Admin Menu, and direct Configurator launch.
* **Custom Items** (`imports/user.nss`) — Create, duplicate, toggle, and organize custom context menu actions.
* **Live `.nss` Preview & Service Tools** — Automatic timestamped backups before saving, Register/Unregister Shell, and Restart Explorer.

### Building the Acrylic GUI Locally
```powershell
dotnet publish src/gui/Shell.Configurator/Shell.Configurator.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o src/bin
```
GitHub Actions automatically builds both native `Shell.sln` (`shell.dll`, `shell.exe`) and `ShellConfigurator.exe` into the downloadable `shell-x64` / `shell-x86` / `shell-arm64` artifacts.


Requirements
------------------
  * Microsoft Windows 7/8/10/11 


Documentation
------------------
Browse the [online documentation here.](https://nilesoft.org/docs)

[<img src="https://devin.ai/assets/deepwiki-badge.png" alt="Ask DeepWiki.com" height="20"/>](https://deepwiki.com/moudey/Shell)

Download
------------------
Download the latest version:  
https://nilesoft.org/download

Screenshots
------------------
<p align="center">
<img src="/screenshots/folder-back.png"><img src="/screenshots/file-manage.png"><br>
<img src="/screenshots/view.png"><img src="/screenshots/edit.png"><br>
<img src="/screenshots/terminal.png"><img src="/screenshots/taskbar.png"><br>
<img src="/screenshots/goto2.png"><img src="/screenshots/gradient.png"><br>
<img src="/screenshots/acrylic.png"><br>

<br>
<br>
</p>

Donate
------------------
If you really love Shell and would like to see it continue to improve.

[![Paypal](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.me/nilesoft)
[![BuyMeACoffee](https://img.shields.io/badge/Donate-BuyMeACoffee-yellow.svg)](https://www.buymeacoffee.com/moudey)


