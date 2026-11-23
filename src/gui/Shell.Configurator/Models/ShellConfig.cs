using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shell.Configurator.Models;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ShellConfig : ObservableObject
{
    public GeneralSettings General { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
    public ModifySettings Modify { get; set; } = new();
    public TerminalSettings Terminal { get; set; } = new();
    public FileManageSettings FileManage { get; set; } = new();
    public DevelopSettings Develop { get; set; } = new();
    public GotoSettings Goto { get; set; } = new();
    public TaskbarSettings Taskbar { get; set; } = new();
    public ObservableCollection<CustomMenuItem> CustomItems { get; set; } = new();

    public static ShellConfig CreateDefault() => new();
}

public sealed class GeneralSettings : ObservableObject
{
    private int _priority = 1;
    private bool _excludeNonExplorer = true;
    private int _showDelay = 200;
    private bool _removeDuplicates = true;
    private bool _tipsEnabled = true;
    private string _language = "auto";
    private bool _enablePinUnpinMenu = true;
    private bool _enableMoreOptionsMenu = true;
    private bool _enableThemeImport = true;
    private bool _enableImagesImport = true;
    private bool _enableModifyImport = true;
    private bool _enableTerminalImport = true;
    private bool _enableFileManageImport = true;
    private bool _enableDevelopImport = true;
    private bool _enableGotoImport = true;
    private bool _enableTaskbarImport = true;
    private bool _enableCustomImport = true;

    public int Priority { get => _priority; set => SetProperty(ref _priority, value); }
    public bool ExcludeNonExplorer { get => _excludeNonExplorer; set => SetProperty(ref _excludeNonExplorer, value); }
    public int ShowDelay { get => _showDelay; set => SetProperty(ref _showDelay, Math.Clamp(value, 0, 4000)); }
    public bool RemoveDuplicates { get => _removeDuplicates; set => SetProperty(ref _removeDuplicates, value); }
    public bool TipsEnabled { get => _tipsEnabled; set => SetProperty(ref _tipsEnabled, value); }
    public string Language { get => _language; set => SetProperty(ref _language, value); }
    public bool EnablePinUnpinMenu { get => _enablePinUnpinMenu; set => SetProperty(ref _enablePinUnpinMenu, value); }
    public bool EnableMoreOptionsMenu { get => _enableMoreOptionsMenu; set => SetProperty(ref _enableMoreOptionsMenu, value); }
    public bool EnableThemeImport { get => _enableThemeImport; set => SetProperty(ref _enableThemeImport, value); }
    public bool EnableImagesImport { get => _enableImagesImport; set => SetProperty(ref _enableImagesImport, value); }
    public bool EnableModifyImport { get => _enableModifyImport; set => SetProperty(ref _enableModifyImport, value); }
    public bool EnableTerminalImport { get => _enableTerminalImport; set => SetProperty(ref _enableTerminalImport, value); }
    public bool EnableFileManageImport { get => _enableFileManageImport; set => SetProperty(ref _enableFileManageImport, value); }
    public bool EnableDevelopImport { get => _enableDevelopImport; set => SetProperty(ref _enableDevelopImport, value); }
    public bool EnableGotoImport { get => _enableGotoImport; set => SetProperty(ref _enableGotoImport, value); }
    public bool EnableTaskbarImport { get => _enableTaskbarImport; set => SetProperty(ref _enableTaskbarImport, value); }
    public bool EnableCustomImport { get => _enableCustomImport; set => SetProperty(ref _enableCustomImport, value); }
    public List<string> PreservedCustomImports { get; set; } = new();
}

public sealed class ThemeSettings : ObservableObject
{
    private string _name = "modern";
    private string _darkMode = "auto";
    private string _view = "auto";
    private string _backgroundColor = "auto";
    private bool _useAutoOpacity = true;
    private int _backgroundOpacity = 85;
    private string _backgroundEffect = "auto";
    private bool _borderEnabled = true;
    private int _borderSize = 1;
    private int _borderRadius = 2;
    private bool _shadowEnabled = true;
    private int _shadowSize = 5;
    private int _shadowOpacity = 50;
    private string _fontName = "Segoe UI";
    private int _fontSize = 14;
    private bool _useCustomFont;
    private int _imageAlign = 2;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string DarkMode { get => _darkMode; set => SetProperty(ref _darkMode, value); }
    public string View { get => _view; set => SetProperty(ref _view, value); }
    public string BackgroundColor { get => _backgroundColor; set => SetProperty(ref _backgroundColor, value); }
    public bool UseAutoOpacity { get => _useAutoOpacity; set => SetProperty(ref _useAutoOpacity, value); }
    public int BackgroundOpacity { get => _backgroundOpacity; set => SetProperty(ref _backgroundOpacity, Math.Clamp(value, 0, 100)); }
    public string BackgroundEffect { get => _backgroundEffect; set => SetProperty(ref _backgroundEffect, value); }
    public bool BorderEnabled { get => _borderEnabled; set => SetProperty(ref _borderEnabled, value); }
    public int BorderSize { get => _borderSize; set => SetProperty(ref _borderSize, Math.Clamp(value, 0, 10)); }
    public int BorderRadius { get => _borderRadius; set => SetProperty(ref _borderRadius, Math.Clamp(value, 0, 3)); }
    public bool ShadowEnabled { get => _shadowEnabled; set => SetProperty(ref _shadowEnabled, value); }
    public int ShadowSize { get => _shadowSize; set => SetProperty(ref _shadowSize, Math.Clamp(value, 0, 30)); }
    public int ShadowOpacity { get => _shadowOpacity; set => SetProperty(ref _shadowOpacity, Math.Clamp(value, 0, 100)); }
    public string FontName { get => _fontName; set => SetProperty(ref _fontName, value); }
    public int FontSize { get => _fontSize; set => SetProperty(ref _fontSize, Math.Clamp(value, 6, 32)); }
    public bool UseCustomFont { get => _useCustomFont; set => SetProperty(ref _useCustomFont, value); }
    public int ImageAlign { get => _imageAlign; set => SetProperty(ref _imageAlign, Math.Clamp(value, 0, 2)); }
}

public sealed class ModifySettings : ObservableObject
{
    private bool _removeRestorePreviousVersions = true;
    private bool _removeCastToDevice = true;
    private bool _promoteEmptyRecycleBin = true;
    private bool _groupPinUnpin = true;
    private bool _moveCopyAsPathToFileManage = true;
    private bool _moveCustomizeFolderToFileManage = true;
    private bool _moveTerminalItemsToTerminal = true;

    private bool _moreSendTo = true;
    private bool _moreShare = true;
    private bool _moreCreateShortcut = true;
    private bool _moreSetDesktopBackground = true;
    private bool _moreRotateLeft = true;
    private bool _moreRotateRight = true;
    private bool _moreMapNetworkDrive = true;
    private bool _moreDisconnectNetworkDrive = true;
    private bool _moreFormat = true;
    private bool _moreEject = true;
    private bool _moreGiveAccessTo = true;
    private bool _moreIncludeInLibrary = true;
    private bool _morePrint = true;

    public bool RemoveRestorePreviousVersions { get => _removeRestorePreviousVersions; set => SetProperty(ref _removeRestorePreviousVersions, value); }
    public bool RemoveCastToDevice { get => _removeCastToDevice; set => SetProperty(ref _removeCastToDevice, value); }
    public bool PromoteEmptyRecycleBin { get => _promoteEmptyRecycleBin; set => SetProperty(ref _promoteEmptyRecycleBin, value); }
    public bool GroupPinUnpin { get => _groupPinUnpin; set => SetProperty(ref _groupPinUnpin, value); }
    public bool MoveCopyAsPathToFileManage { get => _moveCopyAsPathToFileManage; set => SetProperty(ref _moveCopyAsPathToFileManage, value); }
    public bool MoveCustomizeFolderToFileManage { get => _moveCustomizeFolderToFileManage; set => SetProperty(ref _moveCustomizeFolderToFileManage, value); }
    public bool MoveTerminalItemsToTerminal { get => _moveTerminalItemsToTerminal; set => SetProperty(ref _moveTerminalItemsToTerminal, value); }

    public bool MoreSendTo { get => _moreSendTo; set => SetProperty(ref _moreSendTo, value); }
    public bool MoreShare { get => _moreShare; set => SetProperty(ref _moreShare, value); }
    public bool MoreCreateShortcut { get => _moreCreateShortcut; set => SetProperty(ref _moreCreateShortcut, value); }
    public bool MoreSetDesktopBackground { get => _moreSetDesktopBackground; set => SetProperty(ref _moreSetDesktopBackground, value); }
    public bool MoreRotateLeft { get => _moreRotateLeft; set => SetProperty(ref _moreRotateLeft, value); }
    public bool MoreRotateRight { get => _moreRotateRight; set => SetProperty(ref _moreRotateRight, value); }
    public bool MoreMapNetworkDrive { get => _moreMapNetworkDrive; set => SetProperty(ref _moreMapNetworkDrive, value); }
    public bool MoreDisconnectNetworkDrive { get => _moreDisconnectNetworkDrive; set => SetProperty(ref _moreDisconnectNetworkDrive, value); }
    public bool MoreFormat { get => _moreFormat; set => SetProperty(ref _moreFormat, value); }
    public bool MoreEject { get => _moreEject; set => SetProperty(ref _moreEject, value); }
    public bool MoreGiveAccessTo { get => _moreGiveAccessTo; set => SetProperty(ref _moreGiveAccessTo, value); }
    public bool MoreIncludeInLibrary { get => _moreIncludeInLibrary; set => SetProperty(ref _moreIncludeInLibrary, value); }
    public bool MorePrint { get => _morePrint; set => SetProperty(ref _morePrint, value); }
}

public sealed class TerminalSettings : ObservableObject
{
    private bool _enableAdminModifier = true;
    private bool _commandPrompt = true;
    private bool _windowsPowerShell = true;
    private bool _windowsTerminal = true;

    public bool EnableAdminModifier { get => _enableAdminModifier; set => SetProperty(ref _enableAdminModifier, value); }
    public bool CommandPrompt { get => _commandPrompt; set => SetProperty(ref _commandPrompt, value); }
    public bool WindowsPowerShell { get => _windowsPowerShell; set => SetProperty(ref _windowsPowerShell, value); }
    public bool WindowsTerminal { get => _windowsTerminal; set => SetProperty(ref _windowsTerminal, value); }
}

public sealed class FileManageSettings : ObservableObject
{
    private bool _copyPathMenu = true;
    private bool _copyMultiplePaths = true;
    private bool _copyFullPath = true;
    private bool _copyShortcutTarget = true;
    private bool _copyParentPath = true;
    private bool _copyFileName = true;
    private bool _copyFileTitle = true;
    private bool _copyFileExtension = true;

    private bool _changeExtension = true;
    private bool _selectMenu = true;
    private bool _takeOwnership = true;
    private bool _showHideMenu = true;
    private bool _attributesMenu = true;
    private bool _registerServerMenu = true;
    private bool _newFolderMenu = true;
    private bool _newFileMenu = true;
    private bool _folderOptions = true;

    public bool CopyPathMenu { get => _copyPathMenu; set => SetProperty(ref _copyPathMenu, value); }
    public bool CopyMultiplePaths { get => _copyMultiplePaths; set => SetProperty(ref _copyMultiplePaths, value); }
    public bool CopyFullPath { get => _copyFullPath; set => SetProperty(ref _copyFullPath, value); }
    public bool CopyShortcutTarget { get => _copyShortcutTarget; set => SetProperty(ref _copyShortcutTarget, value); }
    public bool CopyParentPath { get => _copyParentPath; set => SetProperty(ref _copyParentPath, value); }
    public bool CopyFileName { get => _copyFileName; set => SetProperty(ref _copyFileName, value); }
    public bool CopyFileTitle { get => _copyFileTitle; set => SetProperty(ref _copyFileTitle, value); }
    public bool CopyFileExtension { get => _copyFileExtension; set => SetProperty(ref _copyFileExtension, value); }

    public bool ChangeExtension { get => _changeExtension; set => SetProperty(ref _changeExtension, value); }
    public bool SelectMenu { get => _selectMenu; set => SetProperty(ref _selectMenu, value); }
    public bool TakeOwnership { get => _takeOwnership; set => SetProperty(ref _takeOwnership, value); }
    public bool ShowHideMenu { get => _showHideMenu; set => SetProperty(ref _showHideMenu, value); }
    public bool AttributesMenu { get => _attributesMenu; set => SetProperty(ref _attributesMenu, value); }
    public bool RegisterServerMenu { get => _registerServerMenu; set => SetProperty(ref _registerServerMenu, value); }
    public bool NewFolderMenu { get => _newFolderMenu; set => SetProperty(ref _newFolderMenu, value); }
    public bool NewFileMenu { get => _newFileMenu; set => SetProperty(ref _newFileMenu, value); }
    public bool FolderOptions { get => _folderOptions; set => SetProperty(ref _folderOptions, value); }
}

public sealed class DevelopSettings : ObservableObject
{
    private bool _editorsMenu = true;
    private bool _vsCode = true;
    private bool _notepad = true;

    private bool _dotnetMenu = true;
    private bool _dotnetRunWatchClean = true;
    private bool _dotnetBuild = true;
    private bool _dotnetPublish = true;
    private bool _dotnetEf = true;
    private bool _dotnetHelpInfo = true;

    private bool _reshack;
    private string _reshackPath = @"D:\config\Programs\dev\petools\resource\reshack\reshack.exe";
    private bool _peview;
    private string _peviewPath = @"D:\config\Programs\dev\petools\peview.exe";
    private bool _webServer;
    private string _webServerPath = @"D:\config\Programs\dev\web\HTTPServer\WebServer.exe";

    public bool EditorsMenu { get => _editorsMenu; set => SetProperty(ref _editorsMenu, value); }
    public bool VsCode { get => _vsCode; set => SetProperty(ref _vsCode, value); }
    public bool Notepad { get => _notepad; set => SetProperty(ref _notepad, value); }

    public bool DotnetMenu { get => _dotnetMenu; set => SetProperty(ref _dotnetMenu, value); }
    public bool DotnetRunWatchClean { get => _dotnetRunWatchClean; set => SetProperty(ref _dotnetRunWatchClean, value); }
    public bool DotnetBuild { get => _dotnetBuild; set => SetProperty(ref _dotnetBuild, value); }
    public bool DotnetPublish { get => _dotnetPublish; set => SetProperty(ref _dotnetPublish, value); }
    public bool DotnetEf { get => _dotnetEf; set => SetProperty(ref _dotnetEf, value); }
    public bool DotnetHelpInfo { get => _dotnetHelpInfo; set => SetProperty(ref _dotnetHelpInfo, value); }

    public bool Reshack { get => _reshack; set => SetProperty(ref _reshack, value); }
    public string ReshackPath { get => _reshackPath; set => SetProperty(ref _reshackPath, value); }
    public bool Peview { get => _peview; set => SetProperty(ref _peview, value); }
    public string PeviewPath { get => _peviewPath; set => SetProperty(ref _peviewPath, value); }
    public bool WebServer { get => _webServer; set => SetProperty(ref _webServer, value); }
    public string WebServerPath { get => _webServerPath; set => SetProperty(ref _webServerPath, value); }
}

public sealed class GotoSettings : ObservableObject
{
    private bool _foldersMenu = true;
    private bool _folderWindows = true;
    private bool _folderSystem = true;
    private bool _folderProgramFiles = true;
    private bool _folderProgramFilesX86 = true;
    private bool _folderProgramData = true;
    private bool _folderApplications = true;
    private bool _folderUsers = true;
    private bool _folderDesktop = true;
    private bool _folderDownloads = true;
    private bool _folderPictures = true;
    private bool _folderDocuments = true;
    private bool _folderStartMenu = true;
    private bool _folderProfile = true;
    private bool _folderAppData = true;
    private bool _folderTemp = true;

    private bool _controlPanel = true;
    private bool _allControlPanelItems = true;
    private bool _runDialog = true;

    private bool _windowsSettingsMenu = true;
    private bool _settingsSystem = true;
    private bool _settingsApps = true;
    private bool _settingsPersonalization = true;
    private bool _settingsNetwork = true;
    private bool _settingsUpdateDefender = true;

    public bool FoldersMenu { get => _foldersMenu; set => SetProperty(ref _foldersMenu, value); }
    public bool FolderWindows { get => _folderWindows; set => SetProperty(ref _folderWindows, value); }
    public bool FolderSystem { get => _folderSystem; set => SetProperty(ref _folderSystem, value); }
    public bool FolderProgramFiles { get => _folderProgramFiles; set => SetProperty(ref _folderProgramFiles, value); }
    public bool FolderProgramFilesX86 { get => _folderProgramFilesX86; set => SetProperty(ref _folderProgramFilesX86, value); }
    public bool FolderProgramData { get => _folderProgramData; set => SetProperty(ref _folderProgramData, value); }
    public bool FolderApplications { get => _folderApplications; set => SetProperty(ref _folderApplications, value); }
    public bool FolderUsers { get => _folderUsers; set => SetProperty(ref _folderUsers, value); }
    public bool FolderDesktop { get => _folderDesktop; set => SetProperty(ref _folderDesktop, value); }
    public bool FolderDownloads { get => _folderDownloads; set => SetProperty(ref _folderDownloads, value); }
    public bool FolderPictures { get => _folderPictures; set => SetProperty(ref _folderPictures, value); }
    public bool FolderDocuments { get => _folderDocuments; set => SetProperty(ref _folderDocuments, value); }
    public bool FolderStartMenu { get => _folderStartMenu; set => SetProperty(ref _folderStartMenu, value); }
    public bool FolderProfile { get => _folderProfile; set => SetProperty(ref _folderProfile, value); }
    public bool FolderAppData { get => _folderAppData; set => SetProperty(ref _folderAppData, value); }
    public bool FolderTemp { get => _folderTemp; set => SetProperty(ref _folderTemp, value); }

    public bool ControlPanel { get => _controlPanel; set => SetProperty(ref _controlPanel, value); }
    public bool AllControlPanelItems { get => _allControlPanelItems; set => SetProperty(ref _allControlPanelItems, value); }
    public bool RunDialog { get => _runDialog; set => SetProperty(ref _runDialog, value); }

    public bool WindowsSettingsMenu { get => _windowsSettingsMenu; set => SetProperty(ref _windowsSettingsMenu, value); }
    public bool SettingsSystem { get => _settingsSystem; set => SetProperty(ref _settingsSystem, value); }
    public bool SettingsApps { get => _settingsApps; set => SetProperty(ref _settingsApps, value); }
    public bool SettingsPersonalization { get => _settingsPersonalization; set => SetProperty(ref _settingsPersonalization, value); }
    public bool SettingsNetwork { get => _settingsNetwork; set => SetProperty(ref _settingsNetwork, value); }
    public bool SettingsUpdateDefender { get => _settingsUpdateDefender; set => SetProperty(ref _settingsUpdateDefender, value); }
}

public sealed class TaskbarSettings : ObservableObject
{
    private bool _shellAdminMenu = true;
    private bool _includeConfiguratorShortcut = true;
    private bool _appsMenu = true;
    private bool _appPaint = true;
    private bool _appEdge = true;
    private bool _appCalculator = true;
    private bool _appRegedit = true;

    private bool _windowsMenu = true;
    private bool _showDesktop = true;
    private bool _windowsSettings = true;
    private bool _taskManager = true;
    private bool _taskbarSettings = true;
    private bool _restartExplorerOnShift = true;

    public bool ShellAdminMenu { get => _shellAdminMenu; set => SetProperty(ref _shellAdminMenu, value); }
    public bool IncludeConfiguratorShortcut { get => _includeConfiguratorShortcut; set => SetProperty(ref _includeConfiguratorShortcut, value); }
    public bool AppsMenu { get => _appsMenu; set => SetProperty(ref _appsMenu, value); }
    public bool AppPaint { get => _appPaint; set => SetProperty(ref _appPaint, value); }
    public bool AppEdge { get => _appEdge; set => SetProperty(ref _appEdge, value); }
    public bool AppCalculator { get => _appCalculator; set => SetProperty(ref _appCalculator, value); }
    public bool AppRegedit { get => _appRegedit; set => SetProperty(ref _appRegedit, value); }

    public bool WindowsMenu { get => _windowsMenu; set => SetProperty(ref _windowsMenu, value); }
    public bool ShowDesktop { get => _showDesktop; set => SetProperty(ref _showDesktop, value); }
    public bool WindowsSettings { get => _windowsSettings; set => SetProperty(ref _windowsSettings, value); }
    public bool TaskManager { get => _taskManager; set => SetProperty(ref _taskManager, value); }
    public bool TaskbarSettingsLink { get => _taskbarSettings; set => SetProperty(ref _taskbarSettings, value); }
    public bool RestartExplorerOnShift { get => _restartExplorerOnShift; set => SetProperty(ref _restartExplorerOnShift, value); }
}

public sealed class CustomMenuItem : ObservableObject
{
    private bool _enabled = true;
    private string _title = "Custom Action";
    private string _contextType = "*";
    private string _submenu = "";
    private string _command = "notepad.exe";
    private string _arguments = "\"@sel.path\"";
    private string _icon = @"\uE10A";
    private bool _runAsAdmin;
    private string _separator = "none";

    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string ContextType { get => _contextType; set => SetProperty(ref _contextType, value); }
    public string Submenu { get => _submenu; set => SetProperty(ref _submenu, value); }
    public string Command { get => _command; set => SetProperty(ref _command, value); }
    public string Arguments { get => _arguments; set => SetProperty(ref _arguments, value); }
    public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
    public bool RunAsAdmin { get => _runAsAdmin; set => SetProperty(ref _runAsAdmin, value); }
    public string Separator { get => _separator; set => SetProperty(ref _separator, value); }
}
