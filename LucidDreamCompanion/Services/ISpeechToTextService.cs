using System.Globalization;

namespace LucidDreamCompanion.Services;

public interface ISpeechToTextService
{
    /// <summary>True if the device has an on-device / Google speech recognizer available.</summary>
    bool IsSupported { get; }

    Task<bool> RequestPermissionAsync();

    /// <summary>
    /// Streams the running transcript. Each yielded value is the *cumulative* text recognised
    /// so far for this session, growing as the user speaks — append it after the editor's
    /// pre-recording text. Ends when <paramref name="ct"/> is cancelled.
    /// </summary>
    IAsyncEnumerable<string> StreamRecognitionAsync(CultureInfo locale, CancellationToken ct);
}
