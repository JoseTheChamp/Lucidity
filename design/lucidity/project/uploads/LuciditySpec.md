# Lucid Dream Companion — Implementation Spec

**Target platform:** Android (primary), via .NET MAUI Blazor Hybrid
**Tooling:** Visual Studio 2022, .NET 10, C#, Blazor, SQLite
**Audience:** Claude Code working autonomously in a fresh repo

---

## 0. Conventions for Claude Code

- Use **.NET MAUI Blazor Hybrid** with .NET 10. Project name: `LucidDreamCompanion`.
- All UI in Razor components under `Components/`. Code-behind in `.razor.cs` partial classes when logic exceeds ~20 lines.
- Services in `Services/` with interfaces (`I{Name}Service`) registered in `MauiProgram.cs` as singletons unless stated otherwise.
- Data layer in `Data/` — EF Core with SQLite provider. Migrations enabled.
- Use **nullable reference types** enabled. Treat warnings as errors.
- Logging via `ILogger<T>` — no `Console.WriteLine`.
- After each phase: app must build, run on Android emulator, and the listed acceptance criteria must pass. Commit with message `feat: complete Phase N — <name>`.
- When a phase introduces a permission, add it to `Platforms/Android/AndroidManifest.xml` and request at runtime on first use.
- **Do not skip ahead.** If something would be easier to build by pulling forward Phase X+1 work, ask first.

---

## 1. Data Model

EF Core entities. SQLite database file at `FileSystem.AppDataDirectory/lucid.db`.

```csharp
// Core journaling
public class DreamEntry
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }      // UTC
    public DateTime DreamDate { get; set; }      // local date the dream "belongs to" (night of)
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public LucidityLevel Lucidity { get; set; }
    public int? SleepQuality { get; set; }        // 1-5, nullable
    public int? SleepSessionId { get; set; }
    public SleepSession? SleepSession { get; set; }
    public List<Tag> Tags { get; set; } = new();
}

public enum LucidityLevel
{
    None = 0,
    FaintAwareness = 1,
    BriefLucidity = 2,
    SustainedLucidity = 3,
    FullControl = 4
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";       // unique, case-insensitive
    public string ColorHex { get; set; } = "#888888";
    public bool IsBuiltIn { get; set; }
    public List<DreamEntry> Entries { get; set; } = new();
}

// Sleep tracking
public class SleepSession
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }      // UTC
    public DateTime? EndedAt { get; set; }       // UTC, null while in progress
    public string? Notes { get; set; }
    public int? TechniqueProfileId { get; set; }
    public TechniqueProfile? TechniqueProfile { get; set; }
    public List<DreamEntry> Entries { get; set; } = new();
    public List<ScheduledCueLog> CueLogs { get; set; } = new();
}

// Audio cues
public class AudioCue
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public CueTriggerMode TriggerMode { get; set; }
    public TimeSpan? OffsetFromSleepStart { get; set; }   // for SleepRelative
    public TimeOnly? ClockTime { get; set; }              // for ClockBased
    public TimeOnly? IntervalStart { get; set; }          // for Interval
    public TimeOnly? IntervalEnd { get; set; }
    public TimeSpan? IntervalEvery { get; set; }
    public int AudioAssetId { get; set; }
    public AudioAsset AudioAsset { get; set; } = null!;
    public int VolumePercent { get; set; } = 30;          // 0-100
    public int FadeInSeconds { get; set; } = 5;
    public int LoopCount { get; set; } = 1;
    public string? VibrationPattern { get; set; }         // CSV of ms, e.g. "0,200,100,200"
    public bool Enabled { get; set; } = true;
    public int? CueProfileId { get; set; }
    public CueProfile? CueProfile { get; set; }
}

public enum CueTriggerMode { SleepRelative, ClockBased, Interval }

public class AudioAsset
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public AudioAssetSource Source { get; set; }
    public string PathOrResourceKey { get; set; } = "";   // bundled key OR file path
    public int DurationMs { get; set; }
}

public enum AudioAssetSource { Bundled, UserRecording, ImportedFile }

public class CueProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsTechniqueTemplate { get; set; }         // true if from Technique Mode
    public string? TechniqueKey { get; set; }             // e.g. "WBTB", "FILD", "MILD"
    public List<AudioCue> Cues { get; set; } = new();
}

// Cue execution log (for debugging + stats)
public class ScheduledCueLog
{
    public int Id { get; set; }
    public int SleepSessionId { get; set; }
    public int AudioCueId { get; set; }
    public DateTime ScheduledFor { get; set; }
    public DateTime? FiredAt { get; set; }
    public string? Status { get; set; }                   // "Fired", "Skipped", "Failed"
}

// Settings (single row, Id = 1)
public class AppSettings
{
    public int Id { get; set; }                            // always 1
    public TimeOnly JournalReminderTime1 { get; set; } = new(6, 30);
    public TimeOnly? JournalReminderTime2 { get; set; } = new(8, 0);
    public bool RealityCheckEnabled { get; set; } = false;
    public TimeOnly RealityCheckStart { get; set; } = new(9, 0);
    public TimeOnly RealityCheckEnd { get; set; } = new(22, 0);
    public int RealityCheckCountPerDay { get; set; } = 5;
    public int? ActiveCueProfileId { get; set; }
}
```

**Many-to-many** between `DreamEntry` and `Tag` via EF Core convention (or explicit `DreamEntryTag` join — your call, document the choice).

---

## 2. Project Structure

```
LucidDreamCompanion/
├── Components/
│   ├── Pages/
│   │   ├── Home.razor                  // dashboard
│   │   ├── Journal/
│   │   │   ├── JournalList.razor
│   │   │   ├── EntryEditor.razor
│   │   │   └── EntryDetail.razor
│   │   ├── Cues/
│   │   │   ├── CueProfiles.razor
│   │   │   ├── CueEditor.razor
│   │   │   └── AudioLibrary.razor
│   │   ├── Sleep/
│   │   │   ├── ActiveSession.razor
│   │   │   └── SessionHistory.razor
│   │   ├── Stats.razor
│   │   ├── Techniques.razor
│   │   └── Settings.razor
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   ├── TagPicker.razor
│   │   ├── LucidityPicker.razor
│   │   └── EmptyState.razor
│   └── Routes.razor
├── Services/
│   ├── IDreamJournalService.cs / DreamJournalService.cs
│   ├── ITagService.cs / TagService.cs
│   ├── ISleepSessionService.cs / SleepSessionService.cs
│   ├── IAudioService.cs / AudioService.cs            // playback abstraction
│   ├── ICueSchedulerService.cs / CueSchedulerService.cs
│   ├── INotificationService.cs / NotificationService.cs
│   ├── ISpeechToTextService.cs / SpeechToTextService.cs
│   ├── IStatsService.cs / StatsService.cs
│   └── ITechniqueTemplateService.cs / TechniqueTemplateService.cs
├── Data/
│   ├── LucidDbContext.cs
│   ├── Migrations/
│   └── Seed/
│       ├── DefaultTagsSeeder.cs
│       ├── BundledAudioSeeder.cs
│       └── TechniqueTemplatesSeeder.cs
├── Platforms/
│   ├── Android/
│   │   ├── AndroidManifest.xml
│   │   ├── Services/
│   │   │   ├── CueForegroundService.cs           // long-running audio scheduler
│   │   │   └── AndroidAudioPlayer.cs
│   │   └── Receivers/
│   │       └── CueAlarmReceiver.cs               // AlarmManager callback
│   └── Windows/ (placeholder, not implemented)
├── Resources/
│   └── Raw/Audio/                                // bundled cue sounds
└── MauiProgram.cs
```

---

# Phase 1 — Skeleton & Journal Core

**Goal:** A working app that lets Josef write, tag, and browse dream entries. No audio, no notifications, no sleep tracking yet.

### Deliverables

1. **Project scaffold**
   - Create .NET 10 MAUI Blazor Hybrid project
   - Configure Android target SDK 34, min SDK 26
   - Add NuGet refs: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Extensions.Logging.Debug`, `CommunityToolkit.Maui`, `CommunityToolkit.Maui.Markup`
   - Set up `LucidDbContext` with EF Core SQLite, store DB at `FileSystem.AppDataDirectory/lucid.db`
   - Create initial migration `InitialCreate` with all entities from §1
   - Apply migration on app start in `MauiProgram.cs`

2. **Navigation shell**
   - Bottom nav (or drawer) with: **Journal**, **Cues** (placeholder), **Sleep** (placeholder), **Stats** (placeholder), **Settings**
   - Dark theme by default (mobile, used at night). Use system theme toggle in settings.

3. **Journal CRUD**
   - `JournalList.razor`: list entries newest first, show title (or first 60 chars of body), date, lucidity icon, tag chips
   - Filter bar: tag multi-select, lucidity level filter, date range, full-text search
   - `EntryEditor.razor`: title, body (large auto-growing textarea), lucidity picker (5 levels), sleep quality (1-5 stars), tag picker, dream date (defaults to "last night" = today if before noon else today)
   - **Autosave**: persist on every change with 500ms debounce. Show "Saved" toast subtly.
   - `EntryDetail.razor`: read-only view with edit button
   - Swipe to delete with undo snackbar

4. **Tag management**
   - `TagPicker.razor` reusable component: chip-style multi-select with inline "create new tag" input
   - Settings → Tags page: list, create, rename, recolor, delete (with confirmation if in use)
   - Seed built-in tags on first run: `dream`, `lucid`, `nightmare`, `recurring`, `false awakening`, `bad sleep`, `WBTB`, `FILD`, `MILD`

5. **Empty states**
   - Journal empty: "No dreams logged yet. Keep a pen and paper by your bed — or use this app within 30 seconds of waking."

### Acceptance Criteria

- [ ] App launches on Android emulator without crash
- [ ] Can create, edit, delete dream entries
- [ ] Can create/edit/delete tags
- [ ] Tag filtering and full-text search work
- [ ] Data persists across app restarts
- [ ] No `Console.WriteLine`; all logs via `ILogger`

---

# Phase 2 — Notifications & Reality Checks

**Goal:** App reminds Josef to journal and prompts daytime reality checks.

### Deliverables

1. **Notification infrastructure**
   - Add `Plugin.LocalNotification` NuGet package
   - Initialize in `MauiProgram.cs`
   - Add Android permissions: `POST_NOTIFICATIONS`, `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `RECEIVE_BOOT_COMPLETED`
   - Request notification permission on first launch with a clear "why we need this" screen

2. **`INotificationService`** API:
   ```csharp
   Task ScheduleJournalReminderAsync(TimeOnly time, string tag);
   Task CancelJournalRemindersAsync();
   Task ScheduleRealityCheckBatchAsync(DateOnly date);  // schedules N random pings within window
   Task CancelRealityChecksAsync();
   Task RescheduleAllAsync();                            // called on boot + settings change
   ```
   Implementation uses repeating daily schedule for journal reminders; one-shot for reality checks (re-scheduled daily on app foreground).

3. **Settings UI**
   - Toggle + time picker for Journal Reminder 1 (default 06:30)
   - Toggle + time picker for Journal Reminder 2 (default 08:00, off by default)
   - Toggle for Reality Check pings; if on: count per day (1-10), start time, end time
   - "Test notification" button for each

4. **Notification tap routing**
   - Journal reminder tap → opens `EntryEditor.razor` with new entry, body field focused
   - Reality check tap → opens a simple "Reality Check" screen: "Look at your hands. Read text twice. Push finger through palm. Are you dreaming?" with a "Done" button that logs the check (light stats use later)

5. **Boot persistence**
   - Implement `BootReceiver` to reschedule all notifications after device reboot

### Acceptance Criteria

- [ ] Notifications fire at configured times even when app is closed
- [ ] Notifications survive device reboot
- [ ] Tapping journal reminder opens new entry editor
- [ ] Reality check pings random times within configured window
- [ ] Settings changes immediately apply

---

# Phase 3 — Audio Library & Manual Playback

**Goal:** Audio plumbing works. Josef can browse the bundled library, import files, record voice memos, and play any of them on demand.

### Deliverables

1. **Bundled audio**
   - Add 6-10 `.ogg` or `.mp3` files to `Resources/Raw/Audio/`:
     - `bell-soft.ogg`
     - `bell-tibetan.ogg`
     - `voice-are-you-dreaming.ogg` (placeholder; record/source later)
     - `pink-noise.ogg`
     - `brown-noise.ogg`
     - `binaural-theta.ogg`
   - Use royalty-free sources or generated tones. Document source in `Resources/Raw/Audio/CREDITS.md`.
   - Seed `AudioAsset` rows on first run pointing at these.

2. **`IAudioService`** API:
   ```csharp
   Task<int> PlayAsync(AudioAsset asset, AudioPlaybackOptions options);  // returns playback handle
   Task StopAsync(int handle);
   Task StopAllAsync();
   Task<AudioAsset> ImportFileAsync(Stream file, string displayName);
   Task<AudioAsset> RecordAsync(TimeSpan maxDuration);
   bool IsPlaying { get; }
   event EventHandler<PlaybackEventArgs>? PlaybackFinished;
   ```
   `AudioPlaybackOptions`: volume %, fade-in seconds, loop count, vibration pattern.

3. **Android implementation** (`Platforms/Android/Services/AndroidAudioPlayer.cs`)
   - Use `MediaPlayer` for playback (handles loop and volume) or `ExoPlayer` if preferred
   - Implement fade-in via timer adjusting volume in steps
   - Use `Vibrator` / `VibratorManager` for vibration patterns
   - Request `RECORD_AUDIO` permission for recording

4. **Audio Library UI** (`AudioLibrary.razor`)
   - List view grouped by source: Bundled / My Recordings / Imported
   - Each row: name, duration, play button, delete (only for user assets)
   - "Import file" button → file picker (`FilePicker.PickAsync` with audio types)
   - "Record new" button → recording sheet with timer, max 60s default

5. **Test page** in dev: a hidden route `/dev/audio` with volume slider, fade-in input, loop count, vibration toggle — sanity-check playback works.

### Acceptance Criteria

- [ ] All bundled sounds play
- [ ] Fade-in works smoothly (no zipper noise)
- [ ] Can import an MP3 from device storage; it persists and plays
- [ ] Can record a voice memo; it persists and plays
- [ ] Vibration patterns work
- [ ] Audio stops when app is backgrounded only if explicitly stopped (set up for Phase 4 background play)

---

# Phase 4 — Cue Scheduling (Clock-Based First)

**Goal:** Josef can schedule audio cues to fire at specific clock times overnight, even with the app closed and screen off.

### Deliverables

1. **`CueForegroundService`** (Android foreground service)
   - Persistent low-priority notification ("Lucid Dream Companion is monitoring cues tonight")
   - Started when user activates a Cue Profile for the night, stopped on "End Session" or after the last cue + 30 min buffer
   - Holds a partial wake lock only during actual audio playback, not the whole time

2. **`AlarmManager`-based scheduling**
   - Use `AlarmManager.SetExactAndAllowWhileIdle` for each cue
   - `CueAlarmReceiver` (BroadcastReceiver) starts the foreground service and triggers playback for the specific cue
   - On boot: reschedule all alarms for the active profile (if any session is "in progress")

3. **`ICueSchedulerService`** API:
   ```csharp
   Task ActivateProfileAsync(int cueProfileId, DateTime sleepStartUtc);
   Task DeactivateAsync();
   Task<IReadOnlyList<ScheduledCueLog>> GetUpcomingAsync();
   ```
   For Phase 4: implement only `ClockBased` and `Interval` modes. `SleepRelative` requires sleep session (Phase 5).

4. **Cue editor UI** (`CueEditor.razor`)
   - Label
   - Trigger mode selector — for Phase 4 only show ClockBased and Interval, gray out SleepRelative with "Requires sleep session — see Phase 5"
   - Time pickers per mode
   - Audio asset picker (with preview play button)
   - Volume slider, fade-in seconds, loop count
   - Vibration pattern: presets (None / Pulse / Long / Custom CSV)
   - Enabled toggle

5. **Cue Profile UI** (`CueProfiles.razor`)
   - List of profiles with cue count
   - Create / rename / delete / duplicate
   - "Activate tonight" button → asks to confirm, starts foreground service
   - Show currently active profile prominently on Home dashboard

6. **Cue execution logging**
   - Every fired cue logs to `ScheduledCueLog` with status
   - Surface in a "Last night's cues" section on Home

### Acceptance Criteria

- [ ] Cue scheduled for 5 minutes ahead fires on time with screen off
- [ ] Cue fires after airplane mode toggle (no network dependency)
- [ ] Cue fires after device reboot (if profile still active)
- [ ] Foreground service notification visible while profile is active
- [ ] OnePlus 13R real-device test: cue fires reliably overnight (Josef tests this and reports)
- [ ] Battery optimization exemption prompt shown on first activation

> ⚠️ **Real-device testing note for Claude Code:** Emulators don't perfectly replicate Android's Doze mode and OEM battery killers. After Phase 4 passes emulator tests, ask Josef to run an overnight test before moving to Phase 5.

---

# Phase 5 — Sleep Sessions & Sleep-Relative Cues

**Goal:** Josef presses "Start Sleep" and cues fire at offsets from that moment.

### Deliverables

1. **Session lifecycle**
   - Home dashboard shows large "Start Sleep" button when no active session
   - When pressed: create `SleepSession`, optionally pick a cue profile (or use active default), start cue scheduler with `sleepStartUtc = now`
   - Replace button with "End Sleep" + elapsed time counter
   - On end: prompt for sleep quality, optional notes, then offer "Log dreams now" → jumps to journal editor with session pre-linked

2. **Sleep-relative cue scheduling**
   - When a profile with `SleepRelative` cues is activated, compute absolute times: `sleepStart + offset`
   - Use same `AlarmManager` plumbing as Phase 4

3. **Session history** (`SessionHistory.razor`)
   - List of past sessions: date, duration, quality, dream count, lucid count
   - Tap → detail view with linked entries and cue log

4. **Link dreams to sessions**
   - In `EntryEditor.razor`, dream date logic: if there's an active or just-ended session within last 12h, default to linking it
   - Manual override available

### Acceptance Criteria

- [ ] Start/end session flow works end to end
- [ ] Sleep-relative cue at offset 30s fires 30s after pressing Start
- [ ] Journal entries auto-link to the matching session
- [ ] Session history shows accurate stats

---

# Phase 6 — Voice Journaling

**Goal:** Half-asleep journaling actually works. Speak; don't type.

### Deliverables

1. **`ISpeechToTextService`**
   - Use `CommunityToolkit.Maui.Media.SpeechToText`
   - Request `RECORD_AUDIO` permission
   - API:
     ```csharp
     Task<bool> RequestPermissionAsync();
     IAsyncEnumerable<string> StreamRecognitionAsync(CultureInfo locale, CancellationToken ct);
     ```
   - Locale: default to device locale, allow override in settings (Josef will likely want `cs-CZ` and `en-US`)

2. **Editor integration**
   - Big mic button next to body field
   - Tap to start, tap again to stop
   - Live transcription appends to body
   - Visual indicator: pulsing red dot + waveform

3. **Lock-screen quick capture**
   - Persistent low-priority notification with "Log dream" action (shown only when a recent session ended)
   - Tap action → opens app directly to mic-active editor

### Acceptance Criteria

- [ ] Voice transcription works in English and Czech
- [ ] Transcription is appended live, not at end
- [ ] Mic permission gracefully degraded if denied (button hidden, hint shown)
- [ ] Lock-screen action opens straight to recording

---

# Phase 7 — Technique Mode Templates

**Goal:** One-tap setup for proven lucid dreaming techniques.

### Deliverables

1. **`ITechniqueTemplateService`** seeds these on first run as locked `CueProfile`s with `IsTechniqueTemplate = true`:

   - **WBTB (Wake Back To Bed)**
     - Cue 1: SleepRelative @ 5h00m — bell-tibetan, volume 50%, fade 8s, single play. Label: "Wake for WBTB"
     - Description: "Wake after ~5h sleep, stay up 20-30 min reading lucid dreaming content, return to bed with intent."
     - Steps shown: 5-step guide

   - **MILD (Mnemonic Induction)**
     - No audio cues. Just journal reminders + technique steps.
     - Steps: recall dream → identify dream sign → visualize becoming lucid → repeat mantra "Next time I'm dreaming, I'll remember I'm dreaming"

   - **FILD (Finger Induced)**
     - Cue 1: SleepRelative @ 4h30m — voice "Move your fingers", volume 30%, fade 5s
     - Cue 2: ClockBased — optional follow-up reality check ping 10 min later
     - Steps: wake briefly → lie still → micro-finger movement → reality check

   - **WILD (Wake Initiated)**
     - Cue 1: SleepRelative @ 6h00m — pink-noise loop for 20 min, volume 20%
     - Steps: wake at REM peak → lie still → observe hypnagogia → maintain awareness through transition

   - **SSILD (Senses Initiated)**
     - Cue 1: SleepRelative @ 4h30m — soft bell
     - Steps: cycle attention through sight (eyes closed), sound, body sensation, ~20 reps

2. **`Techniques.razor` page**
   - Card grid of techniques
   - Tap card → detail page with:
     - Overview
     - Step-by-step guide
     - Best paired tags (e.g., MILD pairs with `dream signs`)
     - "Activate tonight" button
     - "Duplicate as editable profile" (creates an unlocked copy)
   - Locked templates can't be edited directly, only duplicated

3. **Onboarding tie-in**
   - First-launch flow includes "Pick a starter technique" step (default: MILD — gentlest)

### Acceptance Criteria

- [ ] All 5 techniques seeded with correct cues
- [ ] Activating a technique sets cue profile + schedules notifications appropriately
- [ ] Duplicate-to-edit works and creates fully editable copy
- [ ] Technique step guides are readable in dark mode

---

# Phase 8 — Stats & Streaks

**Goal:** Show progress. Reward consistency.

### Deliverables

1. **`IStatsService`** computes:
   - Current journaling streak (consecutive days with at least one entry)
   - Longest streak
   - Total entries / total lucid dreams
   - Lucidity rate (% of entries with `Lucidity >= BriefLucidity`)
   - Rolling 30-day and 90-day lucidity rate
   - Most common tags (top 5)
   - Best technique (highest lucidity rate among sessions linked to each technique profile)
   - Reality check completion rate (last 7 days)

2. **`Stats.razor` page**
   - Hero metric: current streak with flame emoji 🔥
   - Big numbers grid: total dreams, total lucid, lucidity %
   - Calendar heatmap (last 90 days): each day colored by max lucidity achieved
   - Line chart: rolling 30-day lucidity rate over time (use a lightweight charting lib — `ChartJs.Blazor` or hand-rolled SVG)
   - Top tags as horizontal bar chart
   - Technique comparison table

3. **Home dashboard tiles**
   - Streak card
   - "Last night" summary (dream count, lucidity, cue fire status)
   - Active profile card
   - Quick-add dream button

4. **Subtle gamification**
   - Streak milestone celebrations: 3, 7, 14, 30, 60, 100 days — confetti animation + toast, no obnoxious popups
   - First-ever lucid dream: special "🌙 First Lucid Dream" badge shown once on detail screen

### Acceptance Criteria

- [ ] All stats compute correctly on test data
- [ ] Heatmap renders correctly with empty days, partial data, future days blank
- [ ] No layout jank on small screens
- [ ] Streak resets at local midnight, not UTC

---

# Phase 9 — Polish & Export

**Goal:** Production-quality details.

### Deliverables

1. **Export**
   - Settings → Export: choose JSON or Markdown
   - JSON: complete data dump including settings, profiles, cues
   - Markdown: one file per entry, frontmatter with metadata, organized in `/dreams/YYYY/MM/`
   - Share via Android share sheet or save to chosen folder

2. **Import**
   - Restore from JSON export
   - Merge or replace prompt

3. **Onboarding**
   - 4-screen first-launch flow:
     1. Welcome + what the app does
     2. Permissions (notifications, audio recording, battery exemption) with skip option
     3. Pick a starter technique
     4. Set journal reminder time

4. **Settings polish**
   - About page: version, build, links
   - Data management: clear logs, vacuum DB, export, delete all data (double-confirm)
   - Audio output: route choice (default speaker / Bluetooth / wired only) — useful for paired sleep headphones

5. **Accessibility pass**
   - All controls have semantic labels
   - Contrast checked
   - Text scales with system font size

6. **Performance**
   - Journal list virtualizes for >100 entries
   - DB indexes on `DreamEntry.DreamDate`, `DreamEntry.CreatedAt`, `Tag.Name`

### Acceptance Criteria

- [ ] Round-trip export → import preserves all data
- [ ] First-launch flow completes in under 60 seconds
- [ ] App passes basic accessibility audit
- [ ] 1000-entry journal scrolls smoothly

---

## Appendix A — Critical Android Caveats

These will bite. Document them clearly in the README.

1. **Doze mode**: From Android 6+, the system aggressively suspends apps when device is idle. `setExactAndAllowWhileIdle` bypasses this but is rate-limited to once per 9 min per app — fine for our use case, but no 3-cues-in-5-minutes.

2. **OEM battery killers**: OxygenOS (OnePlus), MIUI (Xiaomi), Samsung One UI all kill background apps unpredictably. The foreground service helps but isn't bulletproof. On first activation, deep-link to `Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS` and explain why.

3. **Exact alarm permission (Android 12+)**: Must declare `SCHEDULE_EXACT_ALARM` and on 13+ also handle `USE_EXACT_ALARM`. Check `AlarmManager.CanScheduleExactAlarms()` before scheduling; if false, send user to system settings.

4. **Audio focus**: Request `AudioFocusRequest` with `AudioAttributes.USAGE_ALARM` for cues — this plays even with Do Not Disturb on (which is exactly what we want).

5. **Notification channels**: Create three channels — `cues_active` (low priority, persistent), `journal_reminders` (default), `reality_checks` (high priority, vibrate). Done in `MainApplication.OnCreate`.

---

## Appendix B — Definition of Done (every phase)

- All acceptance criteria checked
- No new compiler warnings
- App runs without crashes on Android emulator
- New code has at minimum smoke-test coverage (where testable)
- README updated with new features and any new permission rationale
- Commit message follows: `feat: complete Phase N — <name>` + body listing what was added
- A short demo GIF or screenshot suggested in commit body (Josef captures manually)

---

## Appendix C — What NOT to Build

To prevent scope creep, the following are **explicitly out of scope**:

- Cloud sync, accounts, login
- Social features, sharing dreams publicly
- Sleep stage detection or wearable integration beyond simple notifications
- AI-generated dream interpretation
- iOS support (might come later, keep platform-specific code isolated)
- Multi-language UI (English only for v1; Czech voice input is fine)

---

*End of spec. Estimated total build time with Claude Code: 30–60 hours of focused work across phases, much of which is testing and Android-quirk wrangling.*
