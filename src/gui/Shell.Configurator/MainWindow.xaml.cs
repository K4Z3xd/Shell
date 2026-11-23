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
        _viewModel.AppThemeChanged += OnAppThemeChanged;
        DataContext = _viewModel;

        SourceInitialized += (_, _) => ApplyWindowAcrylic(_viewModel.IsDarkAppTheme);
        StateChanged += OnWindowStateChanged;
    }

    private void OnAppThemeChanged(object? sender, bool isDark)
    {
        App.ApplyPalette(isDark);
        ApplyWindowAcrylic(isDark);
    }

    private void ApplyWindowAcrylic(bool isDark)
    {
        AcrylicHelper.ApplyAcrylic(this, isDark);
    }

    private void NavCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string key })
        {
            _viewModel.SelectCategory(key);
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Nilesoft Shell Directory (containing shell.nss)",
            InitialDirectory = _viewModel.ShellDirectory
        };

        if (dialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _viewModel.ShellDirectory = dialog.FolderName;
        }
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
