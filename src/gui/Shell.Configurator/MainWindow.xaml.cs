using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Shell.Configurator.Interop;
using Shell.Configurator.ViewModels;

namespace Shell.Configurator;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        SourceInitialized += (_, _) => AcrylicHelper.ApplyAcrylic(this, isDark: true);
        Loaded += (_, _) => AcrylicHelper.ApplyAcrylic(this, isDark: true);
        StateChanged += OnWindowStateChanged;
    }

    private void NavCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
            _viewModel.SelectCategory(key);
    }

    private void BrowseImportFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Existing Nilesoft Shell Config Folder",
            InitialDirectory = _viewModel.ImportFolderPath
        };
        if (dialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            _viewModel.ImportFolderPath = dialog.FolderName;
    }

    private void BrowseFreshFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Destination Folder for Fresh Config",
            InitialDirectory = _viewModel.FreshFolderPath
        };
        if (dialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            _viewModel.FreshFolderPath = dialog.FolderName;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaxRestore_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        bool isMax = WindowState == WindowState.Maximized;
        MaxRestoreBtn.Content = isMax ? "\uE923" : "\uE922";
        RootBorder.Padding = isMax ? new Thickness(7) : new Thickness(0);
    }
}
