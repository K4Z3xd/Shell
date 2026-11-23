using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Shell.Configurator.Models;
using Shell.Configurator.Services;

namespace Shell.Configurator.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
    }

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class NavCategory : ObservableObject
{
    private bool _isSelected;
    private string _badge = "";

    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Glyph { get; init; }
    public required string Description { get; init; }

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string Badge { get => _badge; set => SetProperty(ref _badge, value); }
}

public sealed class MainViewModel : ObservableObject
{
    private ShellConfig _config = ShellConfig.CreateDefault();
    private string _shellDirectory;
    private string _selectedPage = "General";
    private bool _hasUnsavedChanges;
    private bool _isDarkAppTheme = true;
    private string _statusMessage = "Ready";
    private string _selectedPreviewFile = "shell.nss";
    private string _previewContent = "";
    private CustomMenuItem? _selectedCustomItem;
    private string _searchQuery = "";

    public MainViewModel()
    {
        _shellDirectory = ShellService.DetectShellDirectory();

        Categories = new ObservableCollection<NavCategory>
        {
            new() { Key = "General", Title = "General & Modules", Glyph = "\uE713", Description = "Global shell behavior, language, and master category toggles", IsSelected = true },
            new() { Key = "Theme", Title = "Theme & Acrylic", Glyph = "\uE790", Description = "Context menu appearance, backdrop effect, borders, and shadow" },
            new() { Key = "Modify", Title = "System Items", Glyph = "\uE70F", Description = "Remove clutter or group built-in Windows context menu entries" },
            new() { Key = "Terminal", Title = "Terminal", Glyph = "\uE756", Description = "Command Prompt, PowerShell, and Windows Terminal shortcuts" },
            new() { Key = "FileManage", Title = "File Manage", Glyph = "\uE8B7", Description = "Copy Path, Attributes, Ownership, New File/Folder, and Register Server" },
            new() { Key = "Develop", Title = "Develop", Glyph = "\uE943", Description = "Editors, dotnet CLI workflows, and external developer tools" },
            new() { Key = "Goto", Title = "Go To", Glyph = "\uE72A", Description = "Quick folder navigation, Control Panel, Run, and Windows Settings" },
            new() { Key = "Taskbar", Title = "Taskbar", Glyph = "\uE75B", Description = "Taskbar quick apps, window layouts, and Shell admin menu" },
            new() { Key = "Custom", Title = "Custom Items", Glyph = "\uE710", Description = "Create your own context menu items saved to imports/user.nss" },
            new() { Key = "Preview", Title = "NSS Preview & Tools", Glyph = "\uE943", Description = "Inspect generated .nss files and manage Nilesoft Shell service" }
        };

        SaveCommand = new RelayCommand(SaveConfiguration);
        ReloadCommand = new RelayCommand(LoadConfiguration);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        ToggleAppThemeCommand = new RelayCommand(ToggleAppTheme);
        AddCustomItemCommand = new RelayCommand(AddCustomItem);
        DuplicateCustomItemCommand = new RelayCommand(DuplicateCustomItem, () => SelectedCustomItem != null);
        RemoveCustomItemCommand = new RelayCommand(RemoveCustomItem, () => SelectedCustomItem != null);
        RegisterShellCommand = new RelayCommand(() => ExecuteShellAction("-register"));
        UnregisterShellCommand = new RelayCommand(() => ExecuteShellAction("-unregister"));
        RestartExplorerCommand = new RelayCommand(RestartExplorer);
        OpenFolderCommand = new RelayCommand(() => ShellService.OpenFolder(ShellDirectory));

        LoadConfiguration();
    }

    public ObservableCollection<NavCategory> Categories { get; }

    public string[] AvailableLanguages { get; } =
    [
        "auto", "en", "ar", "de-DE", "es-ES", "it", "ja", "ko", "no", "pt-BR", "ro", "ru", "sl", "tr", "ua", "zh-CN", "zh-TW"
    ];

    public string[] ThemeNames { get; } = ["modern", "classic", "black", "white", "auto"];
    public string[] DarkModeOptions { get; } = ["auto", "true", "false"];
    public string[] ViewOptions { get; } = ["auto", "compact", "small", "medium", "large", "wide"];
    public string[] EffectOptions { get; } = ["acrylic", "blur", "transparent", "none", "auto"];
    public string[] ContextTypeOptions { get; } = ["*", "file", "dir", "file|dir", "back", "dir.back", "drive", "taskbar", "desktop"];
    public string[] SeparatorOptions { get; } = ["none", "top", "bottom", "both"];

    public string[] PreviewFiles { get; } =
    [
        "shell.nss",
        @"imports\theme.nss",
        @"imports\modify.nss",
        @"imports\terminal.nss",
        @"imports\file-manage.nss",
        @"imports\develop.nss",
        @"imports\goto.nss",
        @"imports\taskbar.nss",
        @"imports\user.nss"
    ];

    public ShellConfig Config
    {
        get => _config;
        private set
        {
            DetachListeners(_config);
            if (SetProperty(ref _config, value))
            {
                AttachListeners(_config);
                OnPropertyChanged(nameof(HasCustomItems));
                RefreshBadgesAndPreview();
            }
        }
    }

    public string ShellDirectory
    {
        get => _shellDirectory;
        set
        {
            if (SetProperty(ref _shellDirectory, value))
            {
                OnPropertyChanged(nameof(HasShellExecutable));
                LoadConfiguration();
            }
        }
    }

    public bool HasShellExecutable => ShellService.HasShellBinary(ShellDirectory);
    public bool HasCustomItems => Config.CustomItems.Count > 0;

    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (SetProperty(ref _selectedPage, value))
            {
                foreach (var c in Categories)
                    c.IsSelected = string.Equals(c.Key, value, StringComparison.OrdinalIgnoreCase);

                if (value == "Preview")
                    RefreshPreviewText();
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public bool IsDarkAppTheme
    {
        get => _isDarkAppTheme;
        set
        {
            if (SetProperty(ref _isDarkAppTheme, value))
            {
                OnPropertyChanged(nameof(AppThemeLabel));
                OnPropertyChanged(nameof(AppThemeGlyph));
                AppThemeChanged?.Invoke(this, value);
            }
        }
    }

    public string AppThemeLabel => IsDarkAppTheme ? "Dark Acrylic" : "Light Acrylic";
    public string AppThemeGlyph => IsDarkAppTheme ? "\uE708" : "\uE706";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedPreviewFile
    {
        get => _selectedPreviewFile;
        set
        {
            if (SetProperty(ref _selectedPreviewFile, value))
                RefreshPreviewText();
        }
    }

    public string PreviewContent
    {
        get => _previewContent;
        private set => SetProperty(ref _previewContent, value);
    }

    public CustomMenuItem? SelectedCustomItem
    {
        get => _selectedCustomItem;
        set
        {
            if (SetProperty(ref _selectedCustomItem, value))
            {
                DuplicateCustomItemCommand.RaiseCanExecuteChanged();
                RemoveCustomItemCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value) && !string.IsNullOrWhiteSpace(value))
            {
                var match = Categories.FirstOrDefault(c =>
                    c.Title.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    c.Description.Contains(value, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    SelectedPage = match.Key;
            }
        }
    }

    public event EventHandler<bool>? AppThemeChanged;

    public RelayCommand SaveCommand { get; }
    public RelayCommand ReloadCommand { get; }
    public RelayCommand ResetDefaultsCommand { get; }
    public RelayCommand ToggleAppThemeCommand { get; }
    public RelayCommand AddCustomItemCommand { get; }
    public RelayCommand DuplicateCustomItemCommand { get; }
    public RelayCommand RemoveCustomItemCommand { get; }
    public RelayCommand RegisterShellCommand { get; }
    public RelayCommand UnregisterShellCommand { get; }
    public RelayCommand RestartExplorerCommand { get; }
    public RelayCommand OpenFolderCommand { get; }

    public void SelectCategory(string key) => SelectedPage = key;

    public void LoadConfiguration()
    {
        try
        {
            Config = NssSerializer.LoadFromDirectory(ShellDirectory);
            SelectedCustomItem = Config.CustomItems.FirstOrDefault();
            HasUnsavedChanges = false;
            bool exists = File.Exists(Path.Combine(ShellDirectory, "shell.nss"));
            StatusMessage = exists
                ? $"Loaded configuration from {ShellDirectory}"
                : $"No shell.nss found in {ShellDirectory} — using defaults (Save will generate files)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading configuration: {ex.Message}";
        }
    }

    public void SaveConfiguration()
    {
        try
        {
            NssSerializer.SaveToDirectory(ShellDirectory, Config, createBackup: true);
            HasUnsavedChanges = false;
            RefreshPreviewText();
            StatusMessage = $"Saved shell.nss & imports to {ShellDirectory} (hold Ctrl + Right-Click in Explorer to reload)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            MessageBox.Show(
                $"Failed to write configuration files to:\n{ShellDirectory}\n\n{ex.Message}\n\nIf Nilesoft Shell is installed in Program Files, try running ShellConfigurator as Administrator.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ResetDefaults()
    {
        var result = MessageBox.Show(
            "Reset all context menu categories, theme settings, and options back to Nilesoft Shell defaults?",
            "Reset to Defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        Config = ShellConfig.CreateDefault();
        SelectedCustomItem = null;
        HasUnsavedChanges = true;
        StatusMessage = "Defaults restored — click Save Configuration to apply.";
    }

    private void ToggleAppTheme() => IsDarkAppTheme = !IsDarkAppTheme;

    private void AddCustomItem()
    {
        var item = new CustomMenuItem
        {
            Title = $"Custom Action {Config.CustomItems.Count + 1}",
            ContextType = "file|dir",
            Command = "notepad.exe",
            Arguments = "\"@sel.path\"",
            Icon = @"\uE10A"
        };
        item.PropertyChanged += OnSubObjectChanged;
        Config.CustomItems.Add(item);
        SelectedCustomItem = item;
        Config.General.EnableCustomImport = true;
        MarkDirty();
    }

    private void DuplicateCustomItem()
    {
        if (SelectedCustomItem == null) return;
        var src = SelectedCustomItem;
        var copy = new CustomMenuItem
        {
            Enabled = src.Enabled,
            Title = src.Title + " (Copy)",
            ContextType = src.ContextType,
            Submenu = src.Submenu,
            Command = src.Command,
            Arguments = src.Arguments,
            Icon = src.Icon,
            RunAsAdmin = src.RunAsAdmin,
            Separator = src.Separator
        };
        copy.PropertyChanged += OnSubObjectChanged;
        Config.CustomItems.Add(copy);
        SelectedCustomItem = copy;
        MarkDirty();
    }

    private void RemoveCustomItem()
    {
        if (SelectedCustomItem == null) return;
        SelectedCustomItem.PropertyChanged -= OnSubObjectChanged;
        int idx = Config.CustomItems.IndexOf(SelectedCustomItem);
        Config.CustomItems.Remove(SelectedCustomItem);
        SelectedCustomItem = Config.CustomItems.Count > 0
            ? Config.CustomItems[Math.Clamp(idx - 1, 0, Config.CustomItems.Count - 1)]
            : null;
        MarkDirty();
    }

    private void ExecuteShellAction(string args)
    {
        var (ok, msg) = ShellService.RunShellExe(ShellDirectory, args, requireAdmin: true);
        StatusMessage = msg;
        if (!ok)
            MessageBox.Show(msg, "Shell Service", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RestartExplorer()
    {
        var confirm = MessageBox.Show(
            "Restart Windows Explorer now to reload Nilesoft Shell?",
            "Restart Explorer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        var (_, msg) = ShellService.RestartExplorer();
        StatusMessage = msg;
    }

    private void AttachListeners(ShellConfig cfg)
    {
        cfg.General.PropertyChanged += OnSubObjectChanged;
        cfg.Theme.PropertyChanged += OnSubObjectChanged;
        cfg.Modify.PropertyChanged += OnSubObjectChanged;
        cfg.Terminal.PropertyChanged += OnSubObjectChanged;
        cfg.FileManage.PropertyChanged += OnSubObjectChanged;
        cfg.Develop.PropertyChanged += OnSubObjectChanged;
        cfg.Goto.PropertyChanged += OnSubObjectChanged;
        cfg.Taskbar.PropertyChanged += OnSubObjectChanged;
        foreach (var item in cfg.CustomItems)
            item.PropertyChanged += OnSubObjectChanged;
    }

    private void DetachListeners(ShellConfig cfg)
    {
        cfg.General.PropertyChanged -= OnSubObjectChanged;
        cfg.Theme.PropertyChanged -= OnSubObjectChanged;
        cfg.Modify.PropertyChanged -= OnSubObjectChanged;
        cfg.Terminal.PropertyChanged -= OnSubObjectChanged;
        cfg.FileManage.PropertyChanged -= OnSubObjectChanged;
        cfg.Develop.PropertyChanged -= OnSubObjectChanged;
        cfg.Goto.PropertyChanged -= OnSubObjectChanged;
        cfg.Taskbar.PropertyChanged -= OnSubObjectChanged;
        foreach (var item in cfg.CustomItems)
            item.PropertyChanged -= OnSubObjectChanged;
    }

    private void OnSubObjectChanged(object? sender, PropertyChangedEventArgs e) => MarkDirty();

    private void MarkDirty()
    {
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(HasCustomItems));
        RefreshBadgesAndPreview();
    }

    private void RefreshBadgesAndPreview()
    {
        SetBadge("General", CountTrue(
            Config.General.EnableThemeImport, Config.General.EnableImagesImport, Config.General.EnableModifyImport,
            Config.General.EnableTerminalImport, Config.General.EnableFileManageImport, Config.General.EnableDevelopImport,
            Config.General.EnableGotoImport, Config.General.EnableTaskbarImport) + "/8");

        SetBadge("Theme", Config.Theme.BackgroundEffect);

        SetBadge("Modify", Config.General.EnableModifyImport
            ? CountTrue(
                Config.Modify.RemoveRestorePreviousVersions, Config.Modify.RemoveCastToDevice, Config.Modify.PromoteEmptyRecycleBin,
                Config.Modify.GroupPinUnpin, Config.Modify.MoveCopyAsPathToFileManage, Config.Modify.MoveCustomizeFolderToFileManage,
                Config.Modify.MoveTerminalItemsToTerminal, Config.Modify.MoreSendTo, Config.Modify.MoreShare,
                Config.Modify.MoreCreateShortcut, Config.Modify.MoreSetDesktopBackground, Config.Modify.MoreRotateLeft,
                Config.Modify.MoreRotateRight, Config.Modify.MoreMapNetworkDrive, Config.Modify.MoreDisconnectNetworkDrive,
                Config.Modify.MoreFormat, Config.Modify.MoreEject, Config.Modify.MoreGiveAccessTo,
                Config.Modify.MoreIncludeInLibrary, Config.Modify.MorePrint) + "/20"
            : "OFF");

        SetBadge("Terminal", Config.General.EnableTerminalImport
            ? CountTrue(Config.Terminal.CommandPrompt, Config.Terminal.WindowsPowerShell, Config.Terminal.WindowsTerminal) + "/3"
            : "OFF");

        SetBadge("FileManage", Config.General.EnableFileManageImport
            ? CountTrue(
                Config.FileManage.CopyPathMenu, Config.FileManage.ChangeExtension, Config.FileManage.SelectMenu,
                Config.FileManage.TakeOwnership, Config.FileManage.ShowHideMenu, Config.FileManage.AttributesMenu,
                Config.FileManage.RegisterServerMenu, Config.FileManage.NewFolderMenu, Config.FileManage.NewFileMenu,
                Config.FileManage.FolderOptions) + "/10"
            : "OFF");

        SetBadge("Develop", Config.General.EnableDevelopImport
            ? CountTrue(
                Config.Develop.EditorsMenu, Config.Develop.DotnetMenu,
                Config.Develop.Reshack, Config.Develop.Peview, Config.Develop.WebServer) + "/5"
            : "OFF");

        SetBadge("Goto", Config.General.EnableGotoImport
            ? CountTrue(
                Config.Goto.FoldersMenu, Config.Goto.ControlPanel, Config.Goto.AllControlPanelItems,
                Config.Goto.RunDialog, Config.Goto.WindowsSettingsMenu) + "/5"
            : "OFF");

        SetBadge("Taskbar", Config.General.EnableTaskbarImport
            ? CountTrue(
                Config.Taskbar.ShellAdminMenu, Config.Taskbar.AppsMenu, Config.Taskbar.WindowsMenu,
                Config.Taskbar.ShowDesktop, Config.Taskbar.WindowsSettings, Config.Taskbar.TaskManager,
                Config.Taskbar.TaskbarSettingsLink, Config.Taskbar.RestartExplorerOnShift) + "/8"
            : "OFF");

        SetBadge("Custom", Config.CustomItems.Count(i => i.Enabled).ToString());
        SetBadge("Preview", "NSS");

        if (SelectedPage == "Preview")
            RefreshPreviewText();
    }

    private void RefreshPreviewText()
    {
        var files = NssSerializer.GenerateAllFiles(Config);
        PreviewContent = files.TryGetValue(SelectedPreviewFile, out string? content)
            ? content
            : string.Empty;
    }

    private void SetBadge(string key, string value)
    {
        var cat = Categories.FirstOrDefault(c => c.Key == key);
        if (cat != null) cat.Badge = value;
    }

    private static int CountTrue(params bool[] values) => values.Count(v => v);
}
