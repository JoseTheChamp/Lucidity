namespace LucidDreamCompanion;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if ANDROID
        // Accessibility: scale in-app text with the device's system font-size setting.
        blazorWebView.BlazorWebViewInitialized += (_, e) =>
        {
            try
            {
                var scale = Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1f;
                var zoom = Math.Clamp((int)(scale * 100), 80, 200);
                if (e.WebView.Settings is { } settings)
                {
                    settings.TextZoom = zoom;
                }
            }
            catch
            {
                // Non-fatal — fall back to the default text size.
            }
        };
#endif
    }
}
