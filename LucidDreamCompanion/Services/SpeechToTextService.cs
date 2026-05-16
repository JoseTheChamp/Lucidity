using System.Globalization;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

/// <summary>
/// Cross-platform speech-to-text. The recognition engine is implemented in the Android partial
/// (AndroidSpeechToText.cs) on top of <c>android.speech.SpeechRecognizer</c>; other platforms
/// get a no-op so the Windows dev build keeps running.
/// </summary>
public partial class SpeechToTextService : ISpeechToTextService
{
    private readonly ILogger<SpeechToTextService> _logger;

    public SpeechToTextService(ILogger<SpeechToTextService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            return status == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Microphone permission request failed");
            return false;
        }
    }

#if !ANDROID
    public bool IsSupported => false;

    public IAsyncEnumerable<string> StreamRecognitionAsync(CultureInfo locale, CancellationToken ct)
        => EmptyStreamAsync();

    private static async IAsyncEnumerable<string> EmptyStreamAsync()
    {
        await Task.CompletedTask;
        yield break;
    }
#endif
}
