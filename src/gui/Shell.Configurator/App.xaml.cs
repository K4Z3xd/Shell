using System.Windows;
using System.Windows.Media;

namespace Shell.Configurator;

public partial class App : Application
{
    public static void ApplyPalette(bool isDark)
    {
        var res = Current.Resources;
        if (isDark)
        {
            SetBrush(res, "WindowTintBrush", "#9E11151E");
            SetBrush(res, "SidebarBrush", "#420C1018");
            SetBrush(res, "TitleBarBrush", "#280A0D14");
            SetBrush(res, "FooterBrush", "#660E121B");
            SetBrush(res, "CardBrush", "#5C1C2331");
            SetBrush(res, "CardHoverBrush", "#78252E40");
            SetBrush(res, "SubCardBrush", "#44161C28");
            SetBrush(res, "InputBrush", "#66141924");
            SetBrush(res, "InputFocusBrush", "#88192030");
            SetBrush(res, "PopupSurfaceBrush", "#EE181E2B");
            SetBrush(res, "BorderBrushSubtle", "#20FFFFFF");
            SetBrush(res, "BorderBrushStrong", "#38FFFFFF");
            SetBrush(res, "TextPrimaryBrush", "#F4F7FB");
            SetBrush(res, "TextSecondaryBrush", "#A6B2C6");
            SetBrush(res, "TextMutedBrush", "#788399");
            SetBrush(res, "AccentBrush", "#4CC2FF");
            SetBrush(res, "AccentHoverBrush", "#6CD0FF");
            SetBrush(res, "AccentPressedBrush", "#30A8E8");
            SetBrush(res, "AccentSoftBrush", "#284CC2FF");
            SetBrush(res, "AccentTextBrush", "#08121A");
            SetBrush(res, "ToggleOffTrackBrush", "#34FFFFFF");
            SetBrush(res, "ToggleOffBorderBrush", "#4AFFFFFF");
            SetBrush(res, "ToggleThumbBrush", "#FFFFFF");
        }
        else
        {
            SetBrush(res, "WindowTintBrush", "#B4EEF3FA");
            SetBrush(res, "SidebarBrush", "#62E3EAF4");
            SetBrush(res, "TitleBarBrush", "#4CE8EEF8");
            SetBrush(res, "FooterBrush", "#8CE6ECF6");
            SetBrush(res, "CardBrush", "#9CFFFFFF");
            SetBrush(res, "CardHoverBrush", "#CCFFFFFF");
            SetBrush(res, "SubCardBrush", "#72F5F8FD");
            SetBrush(res, "InputBrush", "#B8FFFFFF");
            SetBrush(res, "InputFocusBrush", "#E6FFFFFF");
            SetBrush(res, "PopupSurfaceBrush", "#F4F7FAFF");
            SetBrush(res, "BorderBrushSubtle", "#200F172A");
            SetBrush(res, "BorderBrushStrong", "#3C0F172A");
            SetBrush(res, "TextPrimaryBrush", "#141923");
            SetBrush(res, "TextSecondaryBrush", "#4A5568");
            SetBrush(res, "TextMutedBrush", "#6B768A");
            SetBrush(res, "AccentBrush", "#0067C0");
            SetBrush(res, "AccentHoverBrush", "#1578CC");
            SetBrush(res, "AccentPressedBrush", "#00539C");
            SetBrush(res, "AccentSoftBrush", "#1F0067C0");
            SetBrush(res, "AccentTextBrush", "#FFFFFF");
            SetBrush(res, "ToggleOffTrackBrush", "#280F172A");
            SetBrush(res, "ToggleOffBorderBrush", "#480F172A");
            SetBrush(res, "ToggleThumbBrush", "#FFFFFF");
        }
    }

    private static void SetBrush(ResourceDictionary res, string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        res[key] = new SolidColorBrush(color);
    }
}
