#if ANDROID
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Android.Content;
using Android.OS;
using Android.Speech;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public partial class SpeechToTextService
{
    public bool IsSupported
    {
        get
        {
            try { return SpeechRecognizer.IsRecognitionAvailable(Android.App.Application.Context); }
            catch { return false; }
        }
    }

    public async IAsyncEnumerable<string> StreamRecognitionAsync(
        CultureInfo locale,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        SpeechRecognizer? recognizer = null;
        ContinuousListener? listener = null;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                recognizer = SpeechRecognizer.CreateSpeechRecognizer(Android.App.Application.Context);
                if (recognizer is null)
                {
                    channel.Writer.TryComplete();
                    return;
                }
                listener = new ContinuousListener(recognizer, channel.Writer, locale, _logger);
                recognizer.SetRecognitionListener(listener);
                listener.StartListening();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start speech recognizer");
                channel.Writer.TryComplete();
            }
        });

        using var reg = ct.Register(() =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    listener?.Stop();
                    recognizer?.Destroy();
                }
                catch { /* ignore */ }
                channel.Writer.TryComplete();
            });
        });

        try
        {
            await foreach (var text in channel.Reader.ReadAllAsync(CancellationToken.None))
            {
                yield return text;
            }
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    listener?.Stop();
                    recognizer?.Destroy();
                }
                catch { /* ignore */ }
            });
        }
    }

    /// <summary>
    /// Drives one continuous recognition session: SpeechRecognizer ends each utterance on a
    /// pause, so we re-arm it after every result/error to keep listening through the whole
    /// dream recall. Pushes the cumulative transcript into the channel as it grows.
    /// </summary>
    private sealed class ContinuousListener : Java.Lang.Object, IRecognitionListener
    {
        private readonly SpeechRecognizer _recognizer;
        private readonly ChannelWriter<string> _writer;
        private readonly CultureInfo _locale;
        private readonly ILogger _logger;
        private readonly System.Text.StringBuilder _finalized = new();
        private string _partial = "";
        private bool _stopped;

        public ContinuousListener(SpeechRecognizer recognizer, ChannelWriter<string> writer,
            CultureInfo locale, ILogger logger)
        {
            _recognizer = recognizer;
            _writer = writer;
            _locale = locale;
            _logger = logger;
        }

        private Intent BuildIntent()
        {
            var ctx = Android.App.Application.Context;
            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraPartialResults, true);
            intent.PutExtra(RecognizerIntent.ExtraCallingPackage, ctx.PackageName);
            var tag = string.IsNullOrWhiteSpace(_locale.Name)
                ? CultureInfo.CurrentCulture.Name
                : _locale.Name;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                intent.PutExtra(RecognizerIntent.ExtraLanguage, tag);
                intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, tag);
            }
            return intent;
        }

        public void StartListening()
        {
            if (_stopped) return;
            try { _recognizer.StartListening(BuildIntent()); }
            catch (Exception ex) { _logger.LogWarning(ex, "StartListening threw"); }
        }

        public void Stop()
        {
            _stopped = true;
            try { _recognizer.StopListening(); } catch { /* ignore */ }
            try { _recognizer.Cancel(); } catch { /* ignore */ }
            _writer.TryComplete();
        }

        private void Restart()
        {
            if (_stopped) return;
            // Small delay avoids ERROR_RECOGNIZER_BUSY when re-arming straight after a result.
            new Handler(Looper.MainLooper!).PostDelayed(() =>
            {
                if (_stopped) return;
                try { _recognizer.StartListening(BuildIntent()); }
                catch (Exception ex) { _logger.LogWarning(ex, "Restart StartListening threw"); }
            }, 120);
        }

        private void Push() => _writer.TryWrite((_finalized + _partial).Trim());

        public void OnPartialResults(Bundle? partialResults)
        {
            var list = partialResults?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (list is { Count: > 0 } && !string.IsNullOrWhiteSpace(list[0]))
            {
                _partial = list[0]!;
                Push();
            }
        }

        public void OnResults(Bundle? results)
        {
            var list = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
            if (list is { Count: > 0 } && !string.IsNullOrWhiteSpace(list[0]))
            {
                _finalized.Append(list[0]!.Trim()).Append(' ');
            }
            _partial = "";
            Push();
            Restart();
        }

        public void OnError([Android.Runtime.GeneratedEnum] SpeechRecognizerError error)
        {
            // No-speech / timeout / no-match are normal pauses — just keep listening.
            switch (error)
            {
                case SpeechRecognizerError.NoMatch:
                case SpeechRecognizerError.SpeechTimeout:
                case SpeechRecognizerError.RecognizerBusy:
                case SpeechRecognizerError.Client:
                    Restart();
                    break;
                default:
                    _logger.LogWarning("SpeechRecognizer error: {Error}", error);
                    Restart();
                    break;
            }
        }

        public void OnReadyForSpeech(Bundle? @params) { }
        public void OnBeginningOfSpeech() { }
        public void OnEndOfSpeech() { }
        public void OnRmsChanged(float rmsdB) { }
        public void OnBufferReceived(byte[]? buffer) { }
        public void OnEvent(int eventType, Bundle? @params) { }
    }
}
#endif
