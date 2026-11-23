using System.Windows;
using System.Windows.Controls;

namespace Shell.Configurator.Controls;

public partial class ToggleSettingCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ToggleSettingCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ToggleSettingCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(ToggleSettingCard),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsLastProperty =
        DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(ToggleSettingCard),
            new PropertyMetadata(false, OnIsLastChanged));

    public static readonly DependencyProperty BorderThicknessOverrideProperty =
        DependencyProperty.Register(nameof(BorderThicknessOverride), typeof(Thickness), typeof(ToggleSettingCard),
            new PropertyMetadata(new Thickness(0, 0, 0, 1)));

    public ToggleSettingCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool IsLast
    {
        get => (bool)GetValue(IsLastProperty);
        set => SetValue(IsLastProperty, value);
    }

    public Thickness BorderThicknessOverride
    {
        get => (Thickness)GetValue(BorderThicknessOverrideProperty);
        set => SetValue(BorderThicknessOverrideProperty, value);
    }

    private static void OnIsLastChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ToggleSettingCard ctrl)
            ctrl.BorderThicknessOverride = (bool)e.NewValue ? new Thickness(0) : new Thickness(0, 0, 0, 1);
    }
}
