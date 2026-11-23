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
    private bool _isDisabled;

    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Glyph { get; init; }

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public bool IsDisabled { get => _isDisabled; set => SetProperty(ref _isDisabled, value); }
}

public sealed class MainViewModel : ObservableObject
{
    private ShellConfig _config = ShellConfig.CreateDefault();
    private string _shellDirectory;
    private string _selectedPage = "Settings";
    private bool _hasUnsavedChanges;
    private string _statusMessage = "Ready";
    private string _selectedPreviewFile = "shell.nss";
    private string _previewContent = "";
    private CustomMenuItem? _selectedCustomItem;

    // Portable Setup / Import state
    private bool _isSetupVisible;
    private bool _canCancelSetup;
    private string _importFolderPath = "";
    private bool _copyAppToImportFolder;
    private string _freshFolderPath = "";
    private bool _freshCopyBinaries = true;
    private bool _freshRegisterShell;

    public MainViewModel()
    {
        var resolution = ShellService.ResolveStartupDirectory();
        _shellDirectory = resolution.Directory;
        _isSetupVisible = resolution.NeedsSetup;
        _canCancelSetup = !resolution.NeedsSetup;
        _importFolderPath = ShellService.DetectSuggestedDirectory();
        _freshFolderPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        Categories = new ObservableCollection<NavCategory>
        {
            new() { Key = "Settings", Title = "Settings", Glyph = "\uE713", IsSelected = true },
            new() { Key = "Theme", Title = "Theme", Glyph = "\uE790" },
            new() { Key = "Modify", Title = "Modify", Glyph = "\uE70F" },
            new() { Key = "Terminal", Title = "Terminal", Glyph = "\uE756" },
            new() { Key = "FileManage", Title = "File Manage", Glyph = "\uE8B7" },
            new() { Key = "Develop", Title = "Develop", Glyph = "\uE943" },
            new() { Key = "Goto", Title = "Go To", Glyph = "\uE72A" },
            new() { Key = "Taskbar", Title = "Taskbar", Glyph = "\uE75B" },
            new() { Key = "Custom", Title = "Custom", Glyph = "\uE710" },
            new() { Key = "Script", Title = "Script", Glyph = "\uE8A5" }
        };

        SaveCommand = new RelayCommand(SaveConfiguration);
        ReloadCommand = new RelayCommand(LoadConfiguration);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        AddCustomItemCommand = new RelayCommand(AddCustomItem);
        DuplicateCustomItemCommand = new RelayCommand(DuplicateCustomItem, () => SelectedCustomItem != null);
        RemoveCustomItemCommand = new RelayCommand(RemoveCustomItem, () => SelectedCustomItem != null);
        RegisterShellCommand = new RelayCommand(() => ExecuteShellAction("-register"));
        UnregisterShellCommand = new RelayCommand(() => ExecuteShellAction("-unregister"));
        RestartExplorerCommand = new RelayCommand(RestartExplorer);
        OpenFolderCommand = new RelayCommand(() => ShellService.OpenFolder(ShellDirectory));

        OpenSetupCommand = new RelayCommand(OpenSetup);
        CancelSetupCommand = new RelayCommand(() => IsSetupVisible = false, () => CanCancelSetup);
        CompleteImportCommand = new RelayCommand(CompleteImport);
        CompleteFreshSetupCommand = new RelayCommand(CompleteFreshSetup);

        if (!_isSetupVisible)
            LoadConfiguration();
        else
            RefreshModuleStatesAndPreview();
    }

    public ObservableCollection<NavCategory> Categories { get; }

    public string[] AvailableLanguages { get; } =
    [
        "auto", "en", "ar", "de-DE", "es-ES", "it", "ja", "ko", "no", "pt-BR", "ro", "ru", "sl", "tr", "ua", "zh-CN", "zh-TW"
    ];

    public string[] ThemeNames { get; } = ["modern", "classic", "black", "white", "auto"];
    public string[] DarkModeOptions { get; } = ["auto", "true", "false"];
    public string[] ViewOptions { get; } = ["auto", "compact", "small", "medium", "large", "wide"];
    public string[] EffectOptions { get; } = ["auto", "acrylic", "blur", "transparent", "none"];
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
                OnPropertyChanged(nameof(PreservedImportsSummary));
                RefreshModuleStatesAndPreview();
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

    public string PreservedImportsSummary => Config.General.PreservedCustomImports.Count > 0
        ? $"Preserving {Config.General.PreservedCustomImports.Count} custom import(s): {string.Join(", ", Config.General.PreservedCustomImports)}"
        : string.Empty;

    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (SetProperty(ref _selectedPage, value))
            {
                foreach (var c in Categories)
                    c.IsSelected = string.Equals(c.Key, value, StringComparison.OrdinalIgnoreCase);

                if (value == "Script")
                    RefreshPreviewText();
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

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

    // Setup / Portable Import Properties
    public bool IsSetupVisible
    {
        get => _isSetupVisible;
        set => SetProperty(ref _isSetupVisible, value);
    }

    public bool CanCancelSetup
    {
        get => _canCancelSetup;
        set
        {
            if (SetProperty(ref _canCancelSetup, value))
                CancelSetupCommand.RaiseCanExecuteChanged();
        }
    }

    public string ImportFolderPath
    {
        get => _importFolderPath;
        set => SetProperty(ref _importFolderPath, value);
    }

    public bool CopyAppToImportFolder
    {
        get => _copyAppToImportFolder;
        set => SetProperty(ref _copyAppToImportFolder, value);
    }

    public string FreshFolderPath
    {
        get => _freshFolderPath;
        set => SetProperty(ref _freshFolderPath, value);
    }

    public bool FreshCopyBinaries
    {
        get => _freshCopyBinaries;
        set => SetProperty(ref _freshCopyBinaries, value);
    }

    public bool FreshRegisterShell
    {
        get => _freshRegisterShell;
        set => SetProperty(ref _freshRegisterShell, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ReloadCommand { get; }
    public RelayCommand ResetDefaultsCommand { get; }
    public RelayCommand AddCustomItemCommand { get; }
    public RelayCommand DuplicateCustomItemCommand { get; }
    public RelayCommand RemoveCustomItemCommand { get; }
    public RelayCommand RegisterShellCommand { get; }
    public RelayCommand UnregisterShellCommand { get; }
    public RelayCommand RestartExplorerCommand { get; }
    public RelayCommand OpenFolderCommand { get; }

    public RelayCommand OpenSetupCommand { get; }
    public RelayCommand CancelSetupCommand { get; }
    public RelayCommand CompleteImportCommand { get; }
    public RelayCommand CompleteFreshSetupCommand { get; }

    public void SelectCategory(string key) => SelectedPage = key;

    private void OpenSetup()
    {
        ImportFolderPath = ShellDirectory;
        FreshFolderPath = ShellDirectory;
        CanCancelSetup = true;
        IsSetupVisible = true;
    }

    private void CompleteImport()
    {
        string folder = ImportFolderPath.Trim();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            MessageBox.Show("Please select an existing folder containing your Nilesoft Shell config (shell.nss).",
                "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(Path.Combine(folder, "shell.nss")))
        {
            var confirm = MessageBox.Show(
                $"No shell.nss file was found in:\n{folder}\n\nOpen this folder anyway with default settings?",
                "shell.nss Not Found", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
        }

        string copyNote = "";
        if (CopyAppToImportFolder)
        {
            var (ok, msg) = ShellService.CopyPortableAppIntoFolder(folder);
            copyNote = ok ? " — portable files copied into folder" : $" ({msg})";
        }

        ShellService.SavePortablePointer(folder);
        _shellDirectory = folder;
        OnPropertyChanged(nameof(ShellDirectory));
        OnPropertyChanged(nameof(HasShellExecutable));

        LoadConfiguration();
        CanCancelSetup = true;
        IsSetupVisible = false;
        StatusMessage = $"Imported config from {folder}{copyNote}";
    }

    private void CompleteFreshSetup()
    {
        string folder = FreshFolderPath.Trim();
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show("Please choose a destination folder for the fresh configuration.",
                "Select Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (File.Exists(Path.Combine(folder, "shell.nss")))
        {
            var overwrite = MessageBox.Show(
                $"A shell.nss file already exists in:\n{folder}\n\nOverwrite it with a fresh default configuration?",
                "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (overwrite != MessageBoxResult.Yes) return;
        }

        var (ok, msg) = ShellService.InitializeFreshConfig(folder, FreshCopyBinaries, FreshRegisterShell);
        if (!ok)
        {
            MessageBox.Show(msg, "Setup Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _shellDirectory = folder;
        OnPropertyChanged(nameof(ShellDirectory));
        OnPropertyChanged(nameof(HasShellExecutable));
        LoadConfiguration();
        CanCancelSetup = true;
        IsSetupVisible = false;
        StatusMessage = msg;
    }

    public void LoadConfiguration()
    {
        try
        {
            var (loaded, importedOriginal) = NssSerializer.LoadFromDirectoryWithMeta(ShellDirectory);
            Config = loaded;
            SelectedCustomItem = Config.CustomItems.FirstOrDefault();
            HasUnsavedChanges = false;
            StatusMessage = importedOriginal
                ? $"Loaded existing Nilesoft Shell config from {ShellDirectory}"
                : $"Loaded {ShellDirectory}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load error: {ex.Message}";
        }
    }

    public void SaveConfiguration()
    {
        try
        {
            NssSerializer.SaveToDirectory(ShellDirectory, Config, createBackup: true);
            HasUnsavedChanges = false;
            RefreshPreviewText();
            StatusMessage = "Saved. Hold Ctrl + Right-Click in Explorer to reload immediately.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            MessageBox.Show(
                $"Failed to write files to:\n{ShellDirectory}\n\n{ex.Message}\n\nRun as Administrator if saving into Program Files.",
                "Save Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ResetDefaults()
    {
        var result = MessageBox.Show(
            "Reset all options back to Nilesoft Shell defaults?",
            "Reset Defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        Config = ShellConfig.CreateDefault();
        SelectedCustomItem = null;
        HasUnsavedChanges = true;
        StatusMessage = "Defaults restored — click Save to apply.";
    }

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
            MessageBox.Show(msg, "Nilesoft Shell", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RestartExplorer()
    {
        var confirm = MessageBox.Show(
            "Restart Windows Explorer to reload Nilesoft Shell?",
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
        RefreshModuleStatesAndPreview();
    }

    private void RefreshModuleStatesAndPreview()
    {
        SetDisabled("Theme", !Config.General.EnableThemeImport);
        SetDisabled("Modify", !Config.General.EnableModifyImport);
        SetDisabled("Terminal", !Config.General.EnableTerminalImport);
        SetDisabled("FileManage", !Config.General.EnableFileManageImport);
        SetDisabled("Develop", !Config.General.EnableDevelopImport);
        SetDisabled("Goto", !Config.General.EnableGotoImport);
        SetDisabled("Taskbar", !Config.General.EnableTaskbarImport);

        if (SelectedPage == "Script")
            RefreshPreviewText();
    }

    private void RefreshPreviewText()
    {
        var files = NssSerializer.GenerateAllFiles(Config);
        PreviewContent = files.TryGetValue(SelectedPreviewFile, out string? content)
            ? content
            : string.Empty;
    }

    private void SetDisabled(string key, bool disabled)
    {
        var cat = Categories.FirstOrDefault(c => c.Key == key);
        if (cat != null) cat.IsDisabled = disabled;
    }
}
