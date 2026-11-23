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

## GUI Configurator (`ShellConfigurator`)
This fork includes a native Windows **.NET 8 WPF** GUI (`src/gui/Shell.Configurator`) styled with a Dark Acrylic backdrop for managing Nilesoft Shell configurations cleanly:

* **Portable & Import Ready** — Drop `ShellConfigurator.exe` directly into an existing Nilesoft Shell config folder to load and run immediately (no system install required), or use the built-in Setup screen to **Import from Folder** or **Start with Fresh Config**. Deeply parses existing `.nss` files and preserves custom third-party `import` entries.
* **Matches Upstream Structure** — Simple category navigation matching Nilesoft Shell: **Settings**, **Theme**, **Modify**, **Terminal**, **File Manage**, **Develop**, **Go To**, **Taskbar**, **Custom**, and **Script** preview.
* **Safe Saving & Shell Controls** — Automatic timestamped backups before saving, Register/Unregister Shell, and Restart Explorer.

### Building Locally
```powershell
dotnet publish src/gui/Shell.Configurator/Shell.Configurator.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o src/bin
```
GitHub Actions builds native `Shell.sln` (`shell.dll`, `shell.exe`), `ShellConfigurator.exe`, MSI installers, and `shell-portable-<arch>.zip` bundles for `x64`, `x86`, and `arm64`.


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


