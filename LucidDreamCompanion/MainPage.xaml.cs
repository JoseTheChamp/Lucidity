namespace LucidDreamCompanion;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if ANDROID
        blazorWebView.BlazorWebViewInitialized += (_, e) =>
        {
            try
            {
                // Accessibility: scale in-app text with the device's system font-size setting.
                var scale = Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1f;
                var zoom = Math.Clamp((int)(scale * 100), 80, 200);
                if (e.WebView.Settings is { } settings)
                {
                    settings.TextZoom = zoom;
                }

                // Edge-to-edge safe area: feed the status-bar / nav-bar insets into CSS so the
                // app never renders underneath the system bars (modern Android forces edge-to-edge).
                var listener = new SafeAreaInsetsListener(e.WebView);
                e.WebView.SetOnApplyWindowInsetsListener(listener);
                e.WebView.RequestApplyInsets();
            }
            catch
            {
                // Non-fatal — CSS env() fallback still applies.
            }
        };
#endif
    }

#if ANDROID
    private sealed class SafeAreaInsetsListener : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
    {
        private readonly Android.Webkit.WebView _webView;
        private readonly Android.OS.Handler _handler = new(Android.OS.Looper.MainLooper!);

        public SafeAreaInsetsListener(Android.Webkit.WebView webView) => _webView = webView;

        public Android.Views.WindowInsets OnApplyWindowInsets(Android.Views.View v, Android.Views.WindowInsets insets)
        {
            try
            {
                int topPx, bottomPx;
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    var bars = insets.GetInsets(Android.Views.WindowInsets.Type.SystemBars());
                    topPx = bars.Top;
                    bottomPx = bars.Bottom;
                }
                else
                {
                    topPx = insets.SystemWindowInsetTop;
                    bottomPx = insets.SystemWindowInsetBottom;
                }

                var density = v.Resources?.DisplayMetrics?.Density ?? 1f;
                if (density <= 0f) density = 1f;
                var top = (topPx / density).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                var bottom = (bottomPx / density).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                var js =
                    $"document.documentElement.style.setProperty('--safe-top','{top}px');" +
                    $"document.documentElement.style.setProperty('--safe-bottom','{bottom}px');";

                // Apply now, and again shortly after in case the Blazor document is still loading.
                Eval(js);
                _handler.PostDelayed(() => Eval(js), 400);
                _handler.PostDelayed(() => Eval(js), 1200);
            }
            catch
            {
                // ignore — env() fallback covers it
            }
            return insets;
        }

        private void Eval(string js)
        {
            try { _webView.EvaluateJavascript(js, null); }
            catch { /* document not ready yet */ }
        }
    }
#endif
}
