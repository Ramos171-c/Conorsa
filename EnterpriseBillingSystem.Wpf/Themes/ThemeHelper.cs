using MaterialDesignThemes.Wpf;

namespace EnterpriseBillingSystem.Wpf.Themes;

public class ThemeHelper
{
    public void SetTheme(bool isDark)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
        paletteHelper.SetTheme(theme);
    }
}
