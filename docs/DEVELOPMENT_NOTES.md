# Development notes

Phase-by-phase implementation notes for Lucid Dream Companion, following the plan in [LuciditySpec.md](../LuciditySpec.md). For what the app does and how to build it, see the [README](../README.md).

## Status

- **Phase 1 — Skeleton & Journal Core**: ✅ complete
- **Phase 2 — Notifications & Reality Checks**: ✅ complete
- **Phase 3 — Audio Library & Manual Playback**: ✅ complete
- **Phase 4 — Cue Scheduling (clock-based & interval)**: ✅ complete
- **Phase 5 — Sleep Sessions & Sleep-Relative Cues**: ✅ complete
- **Phase 6 — Voice Journaling**: ✅ complete
- **Phase 7 — Technique Mode Templates**: ✅ complete
- **Phase 8 — Stats & Streaks**: ✅ complete
- **Phase 9 — Polish & Export**: ✅ complete

**All 9 phases complete.**

## Build

Open `LucidDreamCompanion.sln` in **Visual Studio 2022 / Insiders** (requires the **.NET MAUI** workload — Android primary, Windows for quick desktop test).

From CLI:

```
dotnet build -f net10.0-android        # Android (primary)
dotnet build -f net10.0-windows10.0.19041.0   # Windows (for desktop dev testing)
```

Database is created automatically on first launch at `FileSystem.AppDataDirectory/lucid.db` via `EnsureCreated()`. (EF migrations are deferred until Phase 5+ when schema changes start landing.)

## Project layout

See `LuciditySpec.md` §2.

## Design tokens

Lifted verbatim from the Claude Design handoff (`design/lucidity/project/tokens.css`) → `LucidDreamCompanion/wwwroot/tokens.css`. Whenever the spec and the design disagree, the spec wins; otherwise the UI should track the design.

## Phase 1 — Notes

- Targets: `net10.0-android` (primary) and `net10.0-windows10.0.19041.0` (for testing on this machine; iOS/MacCatalyst trimmed since we're on Windows).
- Nullable + treat-warnings-as-errors enabled.
- `Microsoft.EntityFrameworkCore.Sqlite` only — `CommunityToolkit.Maui` listed in spec but not actually used until Phase 3 (audio/file picker), so it's deferred to avoid forcing a MAUI runtime upgrade that the installed workload (10.0.1) doesn't yet provide.
- Routes: `/` and `/journal` → list; `/journal/edit/{id}` → editor (autosaves every 500 ms); `/journal/entry/{id}` → detail; `/cues`, `/sleep`, `/stats` → placeholder; `/settings` and `/settings/tags`.

## Phase 2 — Notes

- Uses **Plugin.LocalNotification 14.1.0** (the spec said the same plugin family but the v14 API lives under `Plugin.LocalNotification.Core.Models.*`).
- Three Android notification channels created at `MauiAppBuilder.UseLocalNotification(...)`: `journal_reminders` (default), `reality_checks` (high + vibration), `cues_active` (low, for Phase 4).
- Permissions added to `AndroidManifest.xml`: `POST_NOTIFICATIONS`, `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `RECEIVE_BOOT_COMPLETED`, `VIBRATE`. Boot survival via the plugin's `ScheduledBootReceiver`.
- First-launch flow: `JournalList` checks `Preferences["ldc.onboarding.notifications.done"]` and routes to `/onboarding/notifications` if unset.
- Tap routing: `NotificationActionTapped` → `NotificationRouter` (singleton) → `MainLayout` reads pending route and navigates. Journal tap pre-creates a fresh entry and opens the editor; reality-check tap goes to `/reality-check`.
- Reality-check scheduling: one-shot pings, seeded by `DateOnly.DayNumber` so the times vary day to day but are deterministic per day. Re-fired from `Settings → Save & reschedule` and any time `RescheduleAllAsync` is called.
- New entity `RealityCheckLog` records each tap. Since Phase 1 used `EnsureCreated`, an idempotent `CREATE TABLE IF NOT EXISTS RealityCheckLogs` runs at startup for upgrade-in-place. EF migrations land when schema churn warrants it (Phase 5).

## Phase 3 — Notes

- **Bundled audio** is *generated* at build/setup time by `Resources/Raw/Audio/_generate.ps1` (22 kHz 16-bit PCM mono WAVs — no third-party samples required). Filenames: `bell-soft.wav`, `bell-tibetan.wav`, `voice-are-you-dreaming.wav` (a 2-bell placeholder until a real recording is supplied), `pink-noise.wav`, `brown-noise.wav`, `binaural-theta.wav`. See [Resources/Raw/Audio/CREDITS.md](../LucidDreamCompanion/Resources/Raw/Audio/CREDITS.md). The PS1 + CREDITS are explicitly excluded from `MauiAsset` so they don't ship in the APK.
- **Playback** on Android uses `Android.Media.MediaPlayer` with `AudioAttributes.USAGE_ALARM` (so DnD doesn't mute cues), an `AudioFocusRequest`, smooth fade-in via volume stepping, loop counts, and CSV vibration patterns through `VibratorManager`/`Vibrator`.
- `IAudioService` API matches spec §3: `PlayAsync` returns a handle, `StopAsync(handle)` / `StopAllAsync`, `ImportFileAsync` via `FilePicker.Default`, `RecordAsync` / `StopRecordingAsync` via `MediaRecorder` (AAC/M4A into `AppDataDirectory/audio/recordings/`), `PlaybackFinished` event.
- **Permissions** added to `AndroidManifest.xml`: `RECORD_AUDIO`, `MODIFY_AUDIO_SETTINGS`.
- **Cross-platform**: `AudioService` is a `partial class`. Android partial in `Platforms/Android/Services/AndroidAudioPlayer.cs` provides real implementations; non-Android targets get harmless stubs so the Windows build still runs the journal UI.
- **UI**: `/cues/audio` is the grouped library (Bundled / My recordings / Imported). `/dev/audio` is a sandbox to A/B volume, fade, loop, and vibration patterns. The Cues page (`/cues`) now links to both; the cue-profile builder lands in Phase 4.

## Phase 4 — Notes

- **Scheduling**: `CueSchedulerService` arms one `AlarmManager.SetExactAndAllowWhileIdle` (RTC_WAKEUP) alarm per cue fire-time, plus a stop-alarm at *last cue + 30 min*. Each alarm's `PendingIntent` request code = the `ScheduledCueLog.Id`, so cancellation is exact. Doze-proof, no network dependency.
- **Trigger modes**: ClockBased (one fire at the next occurrence of a clock time) and Interval (every *N* min across a window) are implemented. SleepRelative is greyed out in the editor and skipped by the scheduler — it needs a real sleep session (Phase 5).
- **Foreground service** (`CueForegroundService`, type `mediaPlayback`): persistent low-priority notification ("Monitoring cues tonight") on the `cues_active` channel; started at activation, stopped on deactivate or by the stop-alarm. Holds a partial `WAKE_LOCK` only during actual playback.
- **`CueAlarmReceiver`**: on a cue alarm, starts/wakes the foreground service to play the cue (survives a killed process via the exact-alarm background-start exemption). On the stop-alarm, deactivates.
- **`CueBootReceiver`** (`BOOT_COMPLETED`): re-arms still-pending alarms for the active profile after reboot.
- **Permissions added**: `WAKE_LOCK`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_MEDIA_PLAYBACK`, `REQUEST_IGNORE_BATTERY_OPTIMIZATIONS`. First activation checks `CanScheduleExactAlarms()` and battery-optimization status and deep-links to the relevant system settings (Appendix A §2/§3).
- **Profiles**: every cue belongs to exactly one profile. `/cues/profiles` lists them with Activate / Duplicate / Edit / Delete; `/cues/profiles/{id}` is the detail (cue list, reorder, add); `/cues/profiles/{id}/cue/{cueId}` is the cue editor. Switching the active profile cancels the old profile's pending alarms and arms the new one — other profiles are untouched. Duplicate makes a fully independent (always-editable) copy.
- **Logging**: every armed/fired cue is a `ScheduledCueLog` row (`Scheduled` → `Fired`/`Failed`/`Cancelled`). The Cues landing page shows the active profile, next-up cues, and a "Last night's cues" list. `ScheduledCueLog.SleepSessionId` is nullable (Phase 4 cues run without a session) and the table is recreated on upgrade since it never held rows.
- ⚠️ Emulators don't replicate Doze / OEM battery killers. **Run an overnight test on the OnePlus 13R before Phase 5.**

## Phase 5 — Notes

- **Sleep tab is the session hub** (`/sleep`, `SleepHome.razor`). Idle state: a cue-profile picker + large "Start Sleep" button. Active state: a live `HH:MM:SS` elapsed counter (driven by a `PeriodicTimer`) and upcoming-cue list. End flow: 5-star quality + notes, then "End session" or "End & log dreams".
- **`SleepSessionService`**: `StartAsync` creates a `SleepSession` and — if a profile is chosen — activates the cue scheduler with `sleepStartUtc = session.StartedAt` and the session id. `EndAsync` stamps `EndedAt`/`Quality`/`Notes` and deactivates cues. `SleepSession.Quality` is a new nullable column (added via `EnsureColumnAsync`); the spec §1 model omitted it but Phase 5 requires it.
- **Sleep-relative cues are live**: `CueSchedulerService.ComputeFireTimes` now handles `SleepRelative` (`sleepStart + OffsetFromSleepStart`). `ActivateProfileAsync` gained an optional `sleepSessionId` that stamps every `ScheduledCueLog`, so a session owns its cue log. The cue editor's "Sleep-relative" mode is un-greyed with an hours/minutes offset picker.
- **Journal ↔ session linking**: `DreamJournalService.CreateAsync` auto-links a new entry to a session that's active or ended within 12 h, and sets the dream date to that session's night. `EntryEditor` has a "Sleep session" dropdown for manual override. "End & log dreams" creates an entry that auto-links to the just-ended session.
- **History**: `/sleep/history` lists past sessions with duration, dream/lucid counts and quality; `/sleep/history/{id}` shows the session detail — linked dreams + the full cue log.

## Phase 6 — Notes

- **Speech-to-text is native** (`android.speech.SpeechRecognizer`), not CommunityToolkit.Maui — the CT.Maui package wants a newer MAUI runtime than the installed 10.0.1 workload, and native gives full control over continuous recognition. `SpeechToTextService` is a `partial class` (Android impl in `Platforms/Android/Services/AndroidSpeechToText.cs`); non-Android targets get a no-op.
- **Continuous recognition**: `SpeechRecognizer` ends each utterance on a pause, so `ContinuousListener` re-arms it after every result/error to keep listening through the whole recall. `StreamRecognitionAsync` yields the *cumulative* transcript (via a `Channel<string>`); the editor appends it after the pre-recording text.
- **Editor mic**: big circular mic button under the body field — tap to start/stop. While recording: pulsing-ring button, blinking red dot, animated CSS waveform. Live transcript appends per-utterance. If permission is denied or no recognizer exists, the button hides and a hint shows.
- **Locale**: `AppSettings.SpeechLocale` (new column) — `/settings/voice` lets the user pick English / Czech or device default. Empty = device culture.
- **Lock-screen quick capture**: ending a sleep session posts a low-priority "Log your dream" notification (`NotificationService.ShowDreamCaptureAsync`). Tapping it creates an entry and opens the editor with `?mic=1`, which auto-starts dictation. The notification is cancelled on tap.
- Manifest: added a `<queries>` entry for `android.speech.RecognitionService` so the recognizer is visible on Android 11+.

## Phase 7 — Notes

- **Five technique templates** seeded as locked `CueProfile`s (`IsTechniqueTemplate = true`): WBTB, MILD, FILD, WILD, SSILD. `TechniqueDefinitions.cs` holds the static metadata (overview, step guide, paired tags, cue specs); `TechniqueTemplatesSeeder` creates the profiles + cues on first run (resolving bundled audio assets by key, after `BundledAudioSeeder`).
- **Cue specs**: WBTB = bell @ sleep+5h; MILD = no cues (mental technique); FILD = voice cue @ +4h30m + reality-check bell @ +4h40m; WILD = pink-noise (150 loops ≈ 20 min) @ +6h; SSILD = soft bell @ +4h30m. *Spec note:* the spec listed FILD's 2nd cue as "ClockBased … 10 min later" — ClockBased can't express "10 min after a sleep-relative event", so it's a SleepRelative cue at +4h40m, which is what the spec intends.
- **`/techniques`** — 2-column card grid; **`/techniques/{key}`** — overview, numbered step guide (accent markers, readable in dark mode), cue list, paired tags, "Activate tonight" + "Duplicate as editable profile".
- **Locked**: technique profiles can't be edited. `CueProfiles` already hid Edit/Delete for templates; `ProfileDetail` now also hides cue add/reorder/delete and the per-cue editor link for templates, offering "Duplicate as editable profile" instead. Duplicating always produces an unlocked, fully-editable copy.
- **Onboarding**: a "Pick a starter technique" step (`/onboarding/technique`, MILD pre-selected/recommended) now runs after the notification-permission screen on first launch. Continue lands on the chosen technique's guide.

## Phase 8 — Notes

- **Nav restructure**: the bottom nav is now **Home / Journal / Sleep / Stats / Settings** (matching the design). Cues + Techniques live under the Sleep tab (the Sleep nav item highlights for `/sleep`, `/cues`, `/techniques`). `/` is the Home dashboard; the journal list moved to `/journal` only.
- **`StatsService`** computes everything from spec §8: current + longest journaling streak (consecutive days by local `DreamDate` — resets at local midnight, with a one-day grace so today doesn't break the streak before it's over), totals, all-time / 30-day / 90-day lucidity rate, top-5 tags, per-technique comparison (lucidity rate of dreams in sessions linked to each cue profile), reality-check completion rate (last 7 days vs. the scheduled estimate), a 90-day heatmap, and a rolling-30-day lucidity trend sampled weekly.
- **`/stats`**: 🔥 hero streak, big-number grid, 90-day calendar heatmap (7-row weekday grid, 6-step lucidity colour scale, leading blanks align weekdays), hand-rolled SVG trend line (no charting dependency), top-tags horizontal bars, technique-comparison table, reality-check tile.
- **`/` Home dashboard**: greeting + date, streak card → Stats, last-night session card → session detail, active-profile card → Cues, quick "+ New dream" / Sleep actions, three recent dreams.
- **Gamification**: streak milestones (3/7/14/30/60/100 nights) trigger a CSS confetti burst + toast on the Home screen, tracked via `Preferences` so each milestone celebrates once. The first-ever lucid dream shows a one-time "🌙 First Lucid Dream" badge on its detail screen.

## Phase 9 — Notes

- **Export** (`ExportService`): JSON is a complete backup (DTO graph — tags, audio assets, cue profiles, cues, sessions, dreams, logs, reminders, settings) shared via the Android share sheet. Markdown writes one file per dream with YAML frontmatter into a zip organised `dreams/YYYY/MM/`, also shared.
- **Import**: picks a JSON backup and restores it with full FK re-mapping (old id → new id across all tables). **Merge** appends to the current journal (reusing tags by name); **Replace** wipes everything first (double-confirmed in the UI). Round-trips faithfully.
- **Data management** (`/settings/data`): export, import, clear cue/reality-check logs, `VACUUM` compaction, and "Delete all data" (double-confirm → wipe + re-seed the built-in tags/sounds/techniques so the app stays usable).
- **About** (`/settings/about`): app version/build via `AppInfo`, platform info.
- **4-screen onboarding**: Welcome → Permissions (notifications + microphone, skippable) → Pick a starter technique → Morning reminder time. Sets `ldc.onboarding.done` at the end. Pre-Phase-9 users who already finished the old flow are not re-onboarded.
- **Performance**: the journal list now uses Blazor `<Virtualize>` over a flattened header/entry row list — smooth with thousands of entries. DB indexes on `DreamEntry.DreamDate`, `DreamEntry.CreatedAt`, `Tag.Name`, `ScheduledCueLog.ScheduledFor` were already in place from earlier phases.
- **Accessibility**: icon-only buttons carry `aria-label`s; the dark theme is high-contrast; in-app text scales with the device's system font-size setting (Android `WebView.Settings.TextZoom` driven by `Configuration.FontScale`).
- **Deferred**: the optional "audio output route" picker (default speaker / Bluetooth / wired) from spec §9.4 is **not implemented** — reliable output-device routing on Android is heavily device-specific and the spec flags it as a nice-to-have for paired sleep headphones. Cues already request `USAGE_ALARM` audio focus, which routes correctly to connected headphones in practice. Can be added later if wanted.
